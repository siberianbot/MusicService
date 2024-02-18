﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MusicService.Database;

#nullable disable

namespace MusicService.Migrations
{
    [DbContext(typeof(MediaDbContext))]
    [Migration("20240218091332_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.2");

            modelBuilder.Entity("MusicService.Database.MediaDbEntry", b =>
                {
                    b.Property<string>("SourceFilePath")
                        .HasColumnType("TEXT");

                    b.Property<string>("TargetFilePath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("SourceFilePath");

                    b.HasIndex("SourceFilePath");

                    b.HasIndex("TargetFilePath");

                    b.ToTable("Entries");
                });
#pragma warning restore 612, 618
        }
    }
}