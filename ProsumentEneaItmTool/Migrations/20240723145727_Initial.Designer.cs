﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProsumentEneaItmTool.Model.DataBase;

#nullable disable

namespace ProsumentEneaItmTool.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20240723145727_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.7");

            modelBuilder.Entity("ProsumentEneaItmTool.Domain.ImportFileRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Date")
                        .HasColumnType("TEXT");

                    b.Property<double>("FedVolumeAfterBanancing")
                        .HasColumnType("REAL");

                    b.Property<double>("FedVolumeBeforeBanancing")
                        .HasColumnType("REAL");

                    b.Property<double>("TakenVolumeAfterBanancing")
                        .HasColumnType("REAL");

                    b.Property<double>("TakenVolumeBeforeBanancing")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.ToTable("Blogs");
                });
#pragma warning restore 612, 618
        }
    }
}
