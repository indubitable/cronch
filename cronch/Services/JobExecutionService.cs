using cronch.Models;
using cronch.Models.ViewModels;
using System.Collections.Concurrent;

namespace cronch.Services;

public class JobExecutionService(ILogger<JobExecutionService> _logger, SettingsService _settingsService, IServiceProvider _serviceProvider)
{
	public readonly record struct ExecutionIdentifier(Guid JobId, Guid ExecutionId, DateTimeOffset StartedOn);

	private readonly ConcurrentDictionary<ExecutionIdentifier, Thread> _executions = new();
	private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _execCancellations = new();

	public virtual Dictionary<Guid, DateTimeOffset> GetLatestExecutionsPerJob()
	{
		using var scope = _serviceProvider.CreateScope();
		var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();
		return executionPersistenceService.GetLatestExecutionsPerJob();
	}

	public virtual ExecutionPersistenceService.ExecutionStatistics GetExecutionStatistics(DateTimeOffset from, DateTimeOffset to)
	{
		using var scope = _serviceProvider.CreateScope();
		var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();
		return executionPersistenceService.GetExecutionStatistics(from, to);
	}

	public virtual ExecutionViewModel? GetExecution(Guid executionId)
	{
		using var scope = _serviceProvider.CreateScope();
		var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

		return ConvertExecutionModelToViewModel(executionPersistenceService.GetExecution(executionId));
	}

	public virtual string GetOutputForExecution(Guid executionId)
	{
		using var scope = _serviceProvider.CreateScope();
		var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

		var execution = executionPersistenceService.GetExecution(executionId);
		var outputContents = string.Empty;
		if (execution != null)
		{
			var outputPathname = executionPersistenceService.GetOutputPathName(execution, false);

			try
			{
				if (File.Exists(outputPathname))
				{
					using var reader = new StreamReader(outputPathname, new FileStreamOptions { Access = FileAccess.Read, Mode = FileMode.Open, Share = FileShare.ReadWrite });
					outputContents = reader.ReadToEnd();
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Unable to read stdout/stderr for execution {Id}", executionId);
			}
		}
		return outputContents;
	}

	public virtual List<ExecutionViewModel> GetRecentExecutions(int maxCount, Guid? jobId)
	{
		using var scope = _serviceProvider.CreateScope();
		var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

		return executionPersistenceService.GetRecentExecutions(maxCount, jobId)
			.Select(e => ConvertExecutionModelToViewModel(e)!.Value)
			.ToList();
	}

	public virtual List<ExecutionIdentifier> GetAllRunningExecutions()
	{
		return _executions.Keys.ToList();
	}

	public virtual void TerminateExecution(Guid id)
	{
		if (_execCancellations.TryGetValue(id, out var cancelSource))
		{
			cancelSource.Cancel();
		}
	}

	public virtual Guid ExecuteJob(JobModel jobModel, ExecutionReason reason)
	{
		var execution = ExecutionModel.CreateNew(jobModel.Id, jobModel.Name, reason, ExecutionStatus.Unknown);

		// Check for parallelism limits
		if (jobModel.Parallelism.HasValue)
		{
			var runningJobCount = _executions.Where(kvp => kvp.Key.JobId == jobModel.Id).Count();
			if (runningJobCount >= jobModel.Parallelism.Value)
			{
				// Execution cannot proceed

				_logger.LogInformation("Parallelism limit exceeded for job '{Name}' ({Id}). Skipping execution.", jobModel.Name, jobModel.Id);

				execution.CompletedOn = execution.StartedOn;
				execution.StopReason = TerminationReason.SkippedForParallelism;

				using var scope = _serviceProvider.CreateScope();
				var persistence = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();
				var engine = scope.ServiceProvider.GetRequiredService<ExecutionEngine>();
				var settings = _settingsService.LoadSettings();

				try
				{
					switch (jobModel.MarkParallelSkipAs)
					{
						case JobModel.ParallelSkipProcessing.MarkAsIndeterminate:
							execution.Status = ExecutionStatus.CompletedAsIndeterminate;
							persistence.AddExecution(execution);
							ExecuteRunCompletionScript(execution, jobModel, settings, engine, persistence);
							break;
						case JobModel.ParallelSkipProcessing.MarkAsWarning:
							execution.Status = ExecutionStatus.CompletedAsWarning;
							persistence.AddExecution(execution);
							ExecuteRunCompletionScript(execution, jobModel, settings, engine, persistence);
							break;
						case JobModel.ParallelSkipProcessing.MarkAsError:
							execution.Status = ExecutionStatus.CompletedAsError;
							persistence.AddExecution(execution);
							ExecuteRunCompletionScript(execution, jobModel, settings, engine, persistence);
							break;
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Unable to save execution state for job '{Name}' ({Id}), execution {ExecName}", jobModel.Name, jobModel.Id, execution.GetExecutionName());
				}

				// Leave and never look back
				return execution.Id;
			}
		}

		var cancelSource = new CancellationTokenSource();
		if (!_execCancellations.TryAdd(execution.Id, cancelSource))
		{
			cancelSource.Dispose();
			throw new InvalidOperationException("Unable to execute job: could not register execution token");
		}

		var thread = new Thread(() => PerformExecution(execution, jobModel, cancelSource.Token))
		{
			IsBackground = true
		};

		// Register the execution
		if (!_executions.TryAdd(GetExecutionIdentifierFromModel(execution), thread))
		{
			_execCancellations.TryRemove(execution.Id, out _);
			cancelSource.Dispose();
			throw new InvalidOperationException("Unable to execute job: could not register execution");
		}

		thread.Start();

		return execution.Id;
	}

	private ExecutionViewModel? ConvertExecutionModelToViewModel(ExecutionModel? model)
	{
		if (model == null) return null;

		var currentExecutions = _executions.Keys.ToList();
		ExecutionStatus fixOutdatedRunningStatus(Guid execId, ExecutionStatus es) => (es == ExecutionStatus.Running && !currentExecutions.Any(ce => ce.ExecutionId == execId)) ? ExecutionStatus.Unknown : es;

		return new ExecutionViewModel
		{
			JobId = model.JobId,
			ExecutionId = model.Id,
			JobName = model.JobName,
			StartedOn = model.StartedOn,
			CompletedOn = model.CompletedOn,
			Status = fixOutdatedRunningStatus(model.Id, model.Status),
			StartReason = model.StartReason,
			StopReason = model.StopReason,
		};
	}

	private void PerformExecution(ExecutionModel execution, JobModel jobModel, CancellationToken cancelToken)
	{
		using var scope = _serviceProvider.CreateScope();
		var persistence = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();
		var engine = scope.ServiceProvider.GetRequiredService<ExecutionEngine>();
		var settings = _settingsService.LoadSettings();

		try
		{
			var scriptDefaultDir = GetDefaultScriptLocation(settings);
			var scriptFilePathname = Path.Combine(scriptDefaultDir, Path.GetRandomFileName());
			if (!string.IsNullOrWhiteSpace(jobModel.ScriptFilePathname))
			{
				if (jobModel.ScriptFilePathname.Contains("{0}"))
				{
					scriptFilePathname = Path.Combine(scriptDefaultDir, string.Format(jobModel.ScriptFilePathname.Trim(), Path.GetRandomFileName()));
				}
				else
				{
					scriptFilePathname = Path.Combine(scriptDefaultDir, jobModel.ScriptFilePathname.Trim());
				}
			}

			using var outputStream = File.Open(persistence.GetOutputPathName(execution, true), FileMode.Create, FileAccess.Write, FileShare.Read);
			execution.Status = ExecutionStatus.Running;
			persistence.AddExecution(execution);
			engine.PerformExecution(execution, jobModel, scriptFilePathname, outputStream, true, GetRunEnvVars(execution, jobModel), cancelToken);
			persistence.UpdateExecution(execution);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while executing job '{Name}' ({Id}), execution {ExecName}", jobModel.Name, jobModel.Id, execution.GetExecutionName());
		}
		finally
		{
			if (_execCancellations.TryRemove(execution.Id, out var cancelSource))
			{
				cancelSource.Dispose();
			}
			_executions.TryRemove(GetExecutionIdentifierFromModel(execution), out var _);
		}

		ExecuteRunCompletionScript(execution, jobModel, settings, engine, persistence);
	}

	private void ExecuteRunCompletionScript(ExecutionModel execution, JobModel jobModel, SettingsModel settings, ExecutionEngine executionEngine, ExecutionPersistenceService executionPersistenceService)
	{
		if (
			string.IsNullOrWhiteSpace(settings.CompletionScript) ||
			string.IsNullOrWhiteSpace(settings.CompletionScriptExecutor) ||
			(execution.Status == ExecutionStatus.CompletedAsSuccess && !settings.RunCompletionScriptOnSuccess) ||
			(execution.Status == ExecutionStatus.CompletedAsIndeterminate && !settings.RunCompletionScriptOnIndeterminate) ||
			(execution.Status == ExecutionStatus.CompletedAsWarning && !settings.RunCompletionScriptOnWarning) ||
			(execution.Status == ExecutionStatus.CompletedAsError && !settings.RunCompletionScriptOnError)
		)
		{
			return;
		}

		using var outputStream = new MemoryStream();
		try
		{
			var runCompletionJob = new JobModel
			{
				Id = Guid.Empty,
				Name = "Run Completion",
				Executor = settings.CompletionScriptExecutor,
				ExecutorArgs = settings.CompletionScriptExecutorArgs,
				Script = settings.CompletionScript,
				TimeLimitSecs = SettingsService.MaxCompletionScriptRuntimeSeconds,
			};
			var runCompletionExecution = ExecutionModel.CreateNew(runCompletionJob.Id, runCompletionJob.Name, ExecutionReason.Scheduled, ExecutionStatus.Unknown);
			var envVars = GetRunCompletionEnvVars(execution, jobModel, executionPersistenceService.GetOutputPathName(execution, false));
			using var cancelSource = new CancellationTokenSource();
			executionEngine.PerformExecution(runCompletionExecution, runCompletionJob, Path.Combine(GetDefaultScriptLocation(settings), Path.GetRandomFileName()), outputStream, false, envVars, cancelSource.Token);

			if (runCompletionExecution.StopReason != TerminationReason.Exited || runCompletionExecution.ExitCode != 0)
			{
				outputStream.Position = 0;
				using var reader = new StreamReader(outputStream, leaveOpen: true);
				_logger.LogWarning("Run completion script did not execute successfully for job '{Name}' ({Id}), execution {ExecName}. Script output:\n{Output}", jobModel.Name, jobModel.Id, execution.GetExecutionName(), reader.ReadToEnd());
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while executing run completion script for job '{Name}' ({Id}), execution {ExecName}", jobModel.Name, jobModel.Id, execution.GetExecutionName());
		}
	}

	private static Dictionary<string, string> GetRunEnvVars(ExecutionModel execution, JobModel jobModel)
	{
		return new Dictionary<string, string> {
			{ "CRONCH_JOB_ID", jobModel.Id.ToString("D") },
			{ "CRONCH_JOB_NAME", jobModel.Name },
			{ "CRONCH_EXECUTION_ID", execution.Id.ToString("D") },
		};
	}

	private static Dictionary<string, string> GetRunCompletionEnvVars(ExecutionModel execution, JobModel jobModel, string outputFilePathname)
	{
		return new Dictionary<string, string> {
			{ "CRONCH_JOB_ID", jobModel.Id.ToString("D") },
			{ "CRONCH_JOB_NAME", jobModel.Name },
			{ "CRONCH_EXECUTION_ID", execution.Id.ToString("D") },
			{ "CRONCH_EXECUTION_STARTED_ON", execution.StartedOn.ToUnixTimeSeconds().ToString() },
			{ "CRONCH_EXECUTION_COMPLETED_ON", execution.CompletedOn?.ToUnixTimeSeconds().ToString() ?? string.Empty },
			{ "CRONCH_EXECUTION_EXIT_CODE", execution.ExitCode.HasValue ? execution.ExitCode.Value.ToString() : string.Empty },
			{ "CRONCH_EXECUTION_STATUS", execution.Status.ToString() },
			{ "CRONCH_EXECUTION_STOP_REASON", execution.StopReason?.ToString() ?? string.Empty },
			{ "CRONCH_EXECUTION_INTERNAL_OUTPUT_FILE", outputFilePathname},
		};
	}

	private static ExecutionIdentifier GetExecutionIdentifierFromModel(ExecutionModel execution)
	{
		return new ExecutionIdentifier(execution.JobId, execution.Id, execution.StartedOn);
	}

	private static string GetDefaultScriptLocation(SettingsModel settings)
	{
		return string.IsNullOrWhiteSpace(settings.DefaultScriptFileLocation) ? Path.GetTempPath() : settings.DefaultScriptFileLocation;
	}
}
