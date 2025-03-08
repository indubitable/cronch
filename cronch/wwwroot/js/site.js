// Find and show any toasts
document.querySelectorAll('.toast').forEach(toastEl => new bootstrap.Toast(toastEl).show());

function convertHighlightLanguageToAce(lang) {
    var aceLang = null;
    switch (lang) {
        case 'c':
        case 'cpp':
            aceLang = 'c_cpp';
            break;
        case 'csharp':
            aceLang = 'csharp';
            break;
        case 'go':
            aceLang = 'golang';
            break;
        case 'java':
            aceLang = 'java';
            break;
        case 'javascript':
            aceLang = 'javascript';
            break;
        case 'json':
            aceLang = 'json';
            break;
        case 'kotlin':
            aceLang = 'kotlin';
            break;
        case 'lua':
            aceLang = 'lua';
            break;
        case 'perl':
            aceLang = 'perl';
            break;
        case 'php-template':
        case 'php':
            aceLang = 'php';
            break;
        case 'python-repl':
        case 'python':
            aceLang = 'python';
            break;
        case 'ruby':
            aceLang = 'ruby';
            break;
        case 'typescript':
            aceLang = 'typescript';
            break;
        case 'xml':
            aceLang = 'xml';
            break;
        case 'yaml':
            aceLang = 'yaml';
            break;
        default:
            aceLang = 'sh';
            break;
    }
    return aceLang;
}

function configureCronchScriptEditor(editorElement, hiddenValueElement) {
    var scriptUpdateTimer = null;
    var determineLanguageTimer = null;
    var selectedAceLanguage = convertHighlightLanguageToAce(hljs.highlightAuto(hiddenValueElement.value).language);
    editorElement.setAttribute('detected-language', selectedAceLanguage);
    var editor = ace.edit(editorElement, {
        autoScrollEditorIntoView: true,
        useWorker: false,
        cursorStyle: 'slim',
        useSoftTabs: true,
        tabSize: 2,
        navigateWithinSoftTabs: true,
    });
    editor.setTheme('ace/theme/tomorrow');
    editor.session.setMode(`ace/mode/${selectedAceLanguage}`);
    editor.session.on('change', function () {
        clearTimeout(scriptUpdateTimer);
        scriptUpdateTimer = setTimeout(() => {
            hiddenValueElement.value = editor.session.getValue();
        }, 10);
        clearTimeout(determineLanguageTimer);
        determineLanguageTimer = setTimeout(() => {
            var aceLang = convertHighlightLanguageToAce(hljs.highlightAuto(editor.session.getValue()).language);
            if (aceLang !== selectedAceLanguage) {
                selectedAceLanguage = aceLang;
                editor.session.setMode(`ace/mode/${selectedAceLanguage}`);
                editorElement.setAttribute('detected-language', selectedAceLanguage);
            }
        }, 200);
    });
    editorElement.classList.remove('script-editor-loading');
}
