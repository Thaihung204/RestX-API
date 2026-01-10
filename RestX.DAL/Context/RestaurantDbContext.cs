using Microsoft.EntityFrameworkCore;
using RestX.Models.Common;
using RestX.Models.Customers;
using RestX.Models.Feedbacks;
using RestX.Models.HR;
using RestX.Models.Identity;
using RestX.Models.Inventory;
using RestX.Models.Loyalty;
using RestX.Models.Menu;
using RestX.Models.Notifications;
using RestX.Models.Orders;
using RestX.Models.Promotions;
using RestX.Models.Reservations;
using RestX.Models.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.DAL.Context
{
    public partial class RestaurantDbContext : DbContext
    {
        public RestaurantDbContext()
        {
        }

        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options)
            : base(options)
        {
        }

        #region DbSets - Status System
        public virtual DbSet<StatusType> StatusTypes { get; set; }
        public virtual DbSet<StatusValue> StatusValues { get; set; }
        #endregion

        #region DbSets - Identity & Users
        //public virtual DbSet<User> Users { get; set; }
        //public virtual DbSet<Role> Roles { get; set; }
        //public virtual DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
        #endregion

        #region DbSets - HR & Employees
        public virtual DbSet<Employee> Employees { get; set; }
        public virtual DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }
        #endregion

        #region DbSets - Menu Management
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Dish> Dishes { get; set; }
        public virtual DbSet<DishImage> DishImages { get; set; }
        public virtual DbSet<DishRecipe> DishRecipes { get; set; }
        public virtual DbSet<MealCombo> MealCombos { get; set; }
        public virtual DbSet<ComboDetail> ComboDetails { get; set; }
        #endregion

        #region DbSets - Table Management
        public virtual DbSet<Table> Tables { get; set; }
        public virtual DbSet<Table3DModel> Table3DModels { get; set; }
        public virtual DbSet<TableSession> TableSessions { get; set; }
        #endregion

        #region DbSets - Reservations
        public virtual DbSet<Reservation> Reservations { get; set; }
        public virtual DbSet<ReservationTable> ReservationTables { get; set; }
        #endregion

        #region DbSets - Orders
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderDetail> OrderDetails { get; set; }
        public virtual DbSet<OrderTable> OrderTables { get; set; }
        #endregion

        #region DbSets - Payments
        public virtual DbSet<Payment> Payments { get; set; }
        #endregion

        #region DbSets - Promotions
        public virtual DbSet<Promotion> Promotions { get; set; }
        public virtual DbSet<PromotionApplicableItem> PromotionApplicableItems { get; set; }
        public virtual DbSet<PromotionHistory> PromotionHistories { get; set; }
        #endregion

        #region DbSets - Customers
        public virtual DbSet<Customer> Customers { get; set; }
        #endregion

        #region DbSets - Loyalty Program
        public virtual DbSet<LoyaltyPointBand> LoyaltyPointBands { get; set; }
        public virtual DbSet<PointsTransaction> PointsTransactions { get; set; }
        #endregion

        #region DbSets - Feedbacks
        public virtual DbSet<Feedback> Feedbacks { get; set; }
        public virtual DbSet<FeedbackImage> FeedbackImages { get; set; }
        #endregion

        #region DbSets - Notifications
        public virtual DbSet<Notification> Notifications { get; set; }
        #endregion

        #region DbSets - Inventory
        public virtual DbSet<Supplier> Suppliers { get; set; }
        public virtual DbSet<Ingredient> Ingredients { get; set; }
        public virtual DbSet<InventoryStock> InventoryStocks { get; set; }
        public virtual DbSet<StockTransaction> StockTransactions { get; set; }
        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
            => optionsBuilder.UseSqlServer("Server=.;Database=restx_notenant;User ID=sa;Password=Passw0rd1!;MultipleActiveResultSets=True");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations
            ConfigureStatusSystem(modelBuilder);
            ConfigureIdentity(modelBuilder);
            ConfigureHR(modelBuilder);
            ConfigureMenu(modelBuilder);
            ConfigureTables(modelBuilder);
            ConfigureReservations(modelBuilder);
            ConfigureOrders(modelBuilder);
            ConfigurePayments(modelBuilder);
            ConfigurePromotions(modelBuilder);
            ConfigureCustomers(modelBuilder);
            ConfigureLoyalty(modelBuilder);
            ConfigureFeedbacks(modelBuilder);
            ConfigureInventory(modelBuilder);

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        #region Configuration Methods

        private void ConfigureStatusSystem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StatusType>(entity =>
            {
                entity.ToTable("StatusTypes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();

                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            });

            modelBuilder.Entity<StatusValue>(entity =>
            {
                entity.ToTable("StatusValues");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.StatusTypeId, e.Code });

                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.ColorCode).HasMaxLength(7);

                entity.HasOne<StatusType>(e => e.StatusType)
                    .WithMany(st => st.StatusValues)
                    .HasForeignKey(e => e.StatusTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureIdentity(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<User>(entity =>
            //{
            //    entity.ToTable("Users");
            //    entity.HasKey(e => e.Id);
            //    entity.HasIndex(e => e.Email).IsUnique();
            //    entity.HasIndex(e => e.PhoneNumber).IsUnique();

            //    entity.Property(e => e.Email).HasMaxLength(320);
            //    entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
            //    entity.Property(e => e.FullName).HasMaxLength(255).IsRequired();
            //    entity.Property(e => e.PhoneNumber).HasMaxLength(15);
            //    entity.Property(e => e.Avatar).HasMaxLength(500);
            //    entity.Property(e => e.Gender).HasMaxLength(10);
            //});

            //modelBuilder.Entity<Role>(entity =>
            //{
            //    entity.ToTable("Roles");
            //    entity.HasKey(e => e.Id);
            //    entity.HasIndex(e => e.RoleName).IsUnique();

            //    entity.Property(e => e.RoleName).HasMaxLength(100).IsRequired();
            //});

            //modelBuilder.Entity<UserRoleAssignment>(entity =>
            //{
            //    entity.ToTable("UserRoleAssignments");
            //    entity.HasKey(e => e.Id);
            //    entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

            //    entity.HasOne<User>(e => e.User)
            //        .WithMany(u => u.UserRoleAssignments)
            //        .HasForeignKey(e => e.UserId)
            //        .OnDelete(DeleteBehavior.Cascade);

            //    entity.HasOne<Role>(e => e.Role)
            //        .WithMany(r => r.UserRoleAssignments)
            //        .HasForeignKey(e => e.RoleId)
            //        .OnDelete(DeleteBehavior.Cascade);
            //});
        }

        private void ConfigureHR(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employees");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();

                entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Position).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Salary).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SalaryType).HasMaxLength(20).IsRequired();

                //entity.HasOne<User>(e => e.User)
                //    .WithMany(u => u.Employees)
                //    .HasForeignKey(e => e.UserId)
                //    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmployeeSchedule>(entity =>
            {
                entity.ToTable("EmployeeSchedules");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.EmployeeId, e.WorkDate });

                entity.HasOne<Employee>(e => e.Employee)
                    .WithMany(emp => emp.EmployeeSchedules)
                    .HasForeignKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<StatusValue>(e => e.Status)
                    .WithMany()
                    .HasForeignKey(e => e.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureMenu(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);

                entity.HasOne<Category>(e => e.ParentCategory)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Dish>(entity =>
            {
                entity.ToTable("Dishes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CategoryId);

                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Unit).HasMaxLength(20);

                entity.HasOne<Category>(e => e.Category)
                    .WithMany(c => c.Dishes)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DishImage>(entity =>
            {
                entity.ToTable("DishImages");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ImageUrl).HasMaxLength(500).IsRequired();
                entity.Property(e => e.TypeId).HasMaxLength(20);

                entity.HasOne<Dish>(e => e.Dish)
                    .WithMany(d => d.DishImages)
                    .HasForeignKey(e => e.DishId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DishRecipe>(entity =>
            {
                entity.ToTable("DishRecipes");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Quantity).HasColumnType("decimal(10,3)");

                entity.HasOne<Dish>(e => e.Dish)
                    .WithMany(d => d.DishRecipes)
                    .HasForeignKey(e => e.DishId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Ingredient>(e => e.Ingredient)
                    .WithMany(i => i.DishRecipes)
                    .HasForeignKey(e => e.IngredientId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MealCombo>(entity =>
            {
                entity.ToTable("MealCombos");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();

                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
                entity.Property(e => e.BaseCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<ComboDetail>(entity =>
            {
                entity.ToTable("ComboDetails");
                entity.HasKey(e => e.Id);

                entity.HasOne<MealCombo>(e => e.MealCombo)
                    .WithMany(c => c.ComboDetails)
                    .HasForeignKey(e => e.ComboId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Dish>(e => e.Dish)
                    .WithMany(d => d.ComboDetails)
                    .HasForeignKey(e => e.DishId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureTables(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Table>(entity =>
            {
                entity.ToTable("Tables");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();

                entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Type).HasMaxLength(20);
                entity.Property(e => e.Shape).HasMaxLength(20);
                entity.Property(e => e.PositionX).HasColumnType("decimal(8,2)");
                entity.Property(e => e.PositionY).HasColumnType("decimal(8,2)");
                entity.Property(e => e.Width).HasColumnType("decimal(6,2)");
                entity.Property(e => e.Height).HasColumnType("decimal(6,2)");
                entity.Property(e => e.Rotation).HasColumnType("decimal(5,2)");
                entity.Property(e => e.ViewDescription).HasMaxLength(500);
                entity.Property(e => e.DefaultViewUrl).HasMaxLength(500);

                entity.HasOne<StatusValue>(e => e.TableStatus)
                    .WithMany()
                    .HasForeignKey(e => e.TableStatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Table3DModel>(entity =>
            {
                entity.ToTable("Table3DModels");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TableId).IsUnique();

                entity.Property(e => e.ModelUrl).HasMaxLength(500).IsRequired();
                entity.Property(e => e.ModelFormat).HasMaxLength(10);
                entity.Property(e => e.EnvironmentMapUrl).HasMaxLength(500);
                entity.Property(e => e.BackgroundColor).HasMaxLength(7);
                entity.Property(e => e.CameraX).HasColumnType("decimal(8,4)");
                entity.Property(e => e.CameraY).HasColumnType("decimal(8,4)");
                entity.Property(e => e.CameraZ).HasColumnType("decimal(8,4)");
                entity.Property(e => e.CameraFOV).HasColumnType("decimal(5,2)");
                entity.Property(e => e.MinZoom).HasColumnType("decimal(4,2)");
                entity.Property(e => e.MaxZoom).HasColumnType("decimal(4,2)");

                entity.HasOne<Table>(e => e.Table)
                    .WithOne(t => t.Table3DModel)
                    .HasForeignKey<Table3DModel>(e => e.TableId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TableSession>(entity =>
            {
                entity.ToTable("TableSessions");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TableId, e.IsActive });

                entity.HasOne<Table>(e => e.Table)
                    .WithMany(t => t.TableSessions)
                    .HasForeignKey(e => e.TableId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Reservation>(e => e.Reservation)
                    .WithMany(r => r.TableSessions)
                    .HasForeignKey(e => e.ReservationId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Order>(e => e.CurrentOrder)
                    .WithMany()
                    .HasForeignKey(e => e.CurrentOrderId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureReservations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.ToTable("Reservations");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.CustomerId, e.Time });
                entity.Property(e => e.SpecialRequests).HasMaxLength(1000);
                entity.Property(e => e.DepositAmount).HasColumnType("decimal(18,2)");

                entity.HasOne<Customer>(e => e.Customer)
                    .WithMany(c => c.Reservations)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<StatusValue>(e => e.ReservationStatus)
                    .WithMany()
                    .HasForeignKey(e => e.ReservationStatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ReservationTable>(entity =>
            {
                entity.ToTable("ReservationTables");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ReservationId, e.TableId }).IsUnique();

                entity.HasOne<Reservation>(e => e.Reservation)
                    .WithMany(r => r.ReservationTables)
                    .HasForeignKey(e => e.ReservationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Table>(e => e.Table)
                    .WithMany(t => t.ReservationTables)
                    .HasForeignKey(e => e.TableId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureOrders(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Reference).IsUnique();
                entity.HasIndex(e => e.CustomerId);

                entity.Property(e => e.Reference).HasMaxLength(20).IsRequired();
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ServiceCharge).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");

                entity.HasOne<Customer>(e => e.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Reservation>(e => e.Reservation)
                    .WithMany(r => r.Orders)
                    .HasForeignKey(e => e.ReservationId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<StatusValue>(e => e.OrderStatus)
                    .WithMany()
                    .HasForeignKey(e => e.OrderStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<StatusValue>(e => e.PaymentStatus)
                    .WithMany()
                    .HasForeignKey(e => e.PaymentStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Employee>(e => e.Handler)
                    .WithMany(emp => emp.HandledOrders)
                    .HasForeignKey(e => e.HandledBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.ToTable("OrderDetails");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Note).HasMaxLength(500);

                entity.HasOne<Order>(e => e.Order)
                    .WithMany(o => o.OrderDetails)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Dish>(e => e.Dish)
                    .WithMany(d => d.OrderDetails)
                    .HasForeignKey(e => e.DishId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<StatusValue>(e => e.ItemStatus)
                    .WithMany()
                    .HasForeignKey(e => e.ItemStatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrderTable>(entity =>
            {
                entity.ToTable("OrderTables");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.OrderId, e.TableId }).IsUnique();

                entity.HasOne<Order>(e => e.Order)
                    .WithMany(o => o.OrderTables)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Table>(e => e.Table)
                    .WithMany(t => t.OrderTables)
                    .HasForeignKey(e => e.TableId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigurePayments(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.TransactionId);

                entity.Property(e => e.PaymentMethodId).HasMaxLength(20).IsRequired();
                entity.Property(e => e.TransactionId).HasMaxLength(100);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CashReceive).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Cashback).HasColumnType("decimal(18,2)");

                entity.HasOne<Order>(e => e.Order)
                    .WithMany(o => o.Payments)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Reservation>(e => e.Reservation)
                    .WithMany(r => r.Payments)
                    .HasForeignKey(e => e.ReservationId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<StatusValue>(e => e.PaymentStatus)
                    .WithMany()
                    .HasForeignKey(e => e.PaymentStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Employee>(e => e.Processor)
                    .WithMany(emp => emp.ProcessedPayments)
                    .HasForeignKey(e => e.ProcessedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigurePromotions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.ToTable("Promotions");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();

                entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.DiscountValue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountType).HasMaxLength(20);
                entity.Property(e => e.MaxDiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<PromotionApplicableItem>(entity =>
            {
                entity.ToTable("PromotionApplicableItems");
                entity.HasKey(e => e.Id);

                entity.HasOne<Promotion>(e => e.Promotion)
                    .WithMany(p => p.PromotionApplicableItems)
                    .HasForeignKey(e => e.PromotionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Dish>(e => e.Dish)
                    .WithMany(d => d.PromotionApplicableItems)
                    .HasForeignKey(e => e.DishId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Category>(e => e.Category)
                    .WithMany(c => c.PromotionApplicableItems)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<MealCombo>(e => e.Combo)
                    .WithMany(c => c.PromotionApplicableItems)
                    .HasForeignKey(e => e.ComboId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PromotionHistory>(entity =>
            {
                entity.ToTable("PromotionHistories");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");

                entity.HasOne<Promotion>(e => e.Promotion)
                    .WithMany(p => p.PromotionHistories)
                    .HasForeignKey(e => e.PromotionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Order>(e => e.Order)
                    .WithMany(o => o.PromotionHistories)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureCustomers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ApplicationUserId).IsUnique();

                entity.Property(e => e.MembershipLevel).HasMaxLength(20);

                entity.HasOne(e => e.ApplicationUser)
              .WithOne()
              .HasForeignKey<Customer>(e => e.ApplicationUserId)
              .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureLoyalty(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LoyaltyPointBand>(entity =>
            {
                entity.ToTable("LoyaltyPointBands");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Min, e.Max });

                entity.Property(e => e.Name).HasMaxLength(20).IsRequired();
                entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5,2)");
                entity.Property(e => e.BenefitDescription).HasMaxLength(500).IsRequired();
            });

            modelBuilder.Entity<PointsTransaction>(entity =>
            {
                entity.ToTable("PointsTransactions");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.CustomerId, e.CreatedDate });

                entity.Property(e => e.Type).HasMaxLength(20);
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.HasOne<Customer>(e => e.Customer)
                    .WithMany(c => c.PointsTransactions)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Order>(e => e.Order)
                    .WithMany(o => o.PointsTransactions)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureFeedbacks(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.ToTable("Feedbacks");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.OrderId, e.CustomerId }).IsUnique();

                entity.Property(e => e.Comment).HasMaxLength(2000);

                entity.HasOne<Order>(e => e.Order)
                    .WithMany(o => o.Feedbacks)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Customer>(e => e.Customer)
                    .WithMany(c => c.Feedbacks)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FeedbackImage>(entity =>
            {
                entity.ToTable("FeedbackImages");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ImageUrl).HasMaxLength(500).IsRequired();

                entity.HasOne<Feedback>(e => e.Feedback)
                    .WithMany(f => f.FeedbackImages)
                    .HasForeignKey(e => e.FeedbackId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureInventory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("Suppliers");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(15);
                entity.Property(e => e.Email).HasMaxLength(320);
                entity.Property(e => e.Address).HasMaxLength(500);
            });

            modelBuilder.Entity<Ingredient>(entity =>
            {
                entity.ToTable("Ingredients");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();

                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Unit).HasMaxLength(20).IsRequired();
                entity.Property(e => e.MinStockLevel).HasColumnType("decimal(10,3)");
                entity.Property(e => e.MaxStockLevel).HasColumnType("decimal(10,3)");
                entity.Property(e => e.Type).HasMaxLength(50);

                entity.HasOne<Supplier>(e => e.Supplier)
                    .WithMany(s => s.Ingredients)
                    .HasForeignKey(e => e.SupplierId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<InventoryStock>(entity =>
            {
                entity.ToTable("InventoryStocks");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IngredientId).IsUnique();

                entity.Property(e => e.CurrentQuantity).HasColumnType("decimal(10,3)");

                entity.HasOne<Ingredient>(e => e.Ingredient)
                    .WithOne(i => i.InventoryStock)
                    .HasForeignKey<InventoryStock>(e => e.IngredientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockTransaction>(entity =>
            {
                entity.ToTable("StockTransactions");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.IngredientId, e.CreatedDate });

                entity.Property(e => e.TransactionType).HasMaxLength(20);
                entity.Property(e => e.Quantity).HasColumnType("decimal(10,3)");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Reference).HasMaxLength(50);

                entity.HasOne<Ingredient>(e => e.Ingredient)
                    .WithMany(i => i.StockTransactions)
                    .HasForeignKey(e => e.IngredientId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        #endregion
    }
}
