﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using cronch;

#nullable disable

namespace cronch.Migrations
{
    [DbContext(typeof(CronchDbContext))]
    [Migration("20231117043524_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("cronch.Models.ExecutionModel", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CompletedOn")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ExitCode")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("JobId")
                        .HasColumnType("TEXT");

                    b.Property<string>("StartReason")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("StartedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("StopReason")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Execution", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
