using Entity;
using Microsoft.EntityFrameworkCore;
using System;

namespace Repository
{
    public class FoodsContext : DbContext
    {
        public FoodsContext(DbContextOptions<FoodsContext> options) : base(options)
        {
        }

        public virtual DbSet<Area> Areas => Set<Area>();
        public virtual DbSet<City> Citys => Set<City>();
        public virtual DbSet<Country> Countrys => Set<Country>();
        public virtual DbSet<District> Districts => Set<District>();
        public virtual DbSet<Gender> Genders => Set<Gender>();
        public virtual DbSet<Item> Items => Set<Item>();
        public virtual DbSet<ItemCategory> ItemCategorys => Set<ItemCategory>();
        public virtual DbSet<ItemCountry> ItemCountrys => Set<ItemCountry>();
        public virtual DbSet<ItemMael> ItemMaels => Set<ItemMael>();
        public virtual DbSet<ItemRawMaterial> ItemRawMaterials => Set<ItemRawMaterial>();
        public virtual DbSet<Job> Jobs => Set<Job>();
        public virtual DbSet<Location> Locations => Set<Location>();
        public virtual DbSet<Mael> Maels => Set<Mael>();
        public virtual DbSet<Occasion> Occasions => Set<Occasion>();
        public virtual DbSet<Order> Orders => Set<Order>();
        public virtual DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public virtual DbSet<OrderRawMaterial> OrderRawMaterials => Set<OrderRawMaterial>();
        public virtual DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
        public virtual DbSet<RawMaterialCategory> RawMaterialCategorys => Set<RawMaterialCategory>();
        public virtual DbSet<RawMaterialUnitMeasure> RawMaterialUnitMeasures => Set<RawMaterialUnitMeasure>();
        public virtual DbSet<RawMaterialUnitMeasurePrice> RawMaterialUnitMeasurePrices => Set<RawMaterialUnitMeasurePrice>();
        public virtual DbSet<Religion> Religions => Set<Religion>();
        public virtual DbSet<UnitMeasure> UnitMeasures => Set<UnitMeasure>();
        public virtual DbSet<User> Users => Set<User>();
    }
}