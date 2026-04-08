using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RefactorStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ════════════════════════════════════════════════════════════════════════════
            // ORDER (enum int)
            // Old: Pending=0, Confirmed=1, Serving=2, Completed=3, Cancelled=4
            // New: Open=0, Completed=1, Cancelled=2
            // ════════════════════════════════════════════════════════════════════════════

            migrationBuilder.Sql("UPDATE Orders SET OrderStatusId = -1 WHERE OrderStatusId IN (0, 1, 2)");
            migrationBuilder.Sql("UPDATE Orders SET OrderStatusId = 1 WHERE OrderStatusId = 3");
            migrationBuilder.Sql("UPDATE Orders SET OrderStatusId = 2 WHERE OrderStatusId = 4");
            migrationBuilder.Sql("UPDATE Orders SET OrderStatusId = 0 WHERE OrderStatusId = -1");
            // Any unknown value → Open=0
            migrationBuilder.Sql("UPDATE Orders SET OrderStatusId = 0 WHERE OrderStatusId NOT IN (0, 1, 2)");

            // ════════════════════════════════════════════════════════════════════════════
            // TABLE (enum int)
            // Old: Available=0, Reserved=1, Occupied=2
            // New: Available=0, Occupied=1
            // ════════════════════════════════════════════════════════════════════════════

            migrationBuilder.Sql("UPDATE Tables SET TableStatusId = -1 WHERE TableStatusId = 2");
            migrationBuilder.Sql("UPDATE Tables SET TableStatusId = 0 WHERE TableStatusId = 1");
            migrationBuilder.Sql("UPDATE Tables SET TableStatusId = 1 WHERE TableStatusId = -1");
            // Any unknown value → Available=0
            migrationBuilder.Sql("UPDATE Tables SET TableStatusId = 0 WHERE TableStatusId NOT IN (0, 1)");

            // ════════════════════════════════════════════════════════════════════════════
            // RESERVATION (StatusValues table)
            // Allowed: PENDING, CONFIRMED, COMPLETED, CANCELLED
            // Strategy for unknown statuses:
            //   - anything "pending-like" (deposit pending, waiting) → PENDING
            //   - anything "no show / absent / missed" → CANCELLED
            //   - anything "seated / active / in progress" → CONFIRMED
            //   - everything else unknown → CANCELLED
            // ════════════════════════════════════════════════════════════════════════════

            // DEPOSIT_PENDING → PENDING
            // If PENDING already exists: migrate reservations from DEPOSIT_PENDING → existing PENDING, then delete DEPOSIT_PENDING
            // If PENDING doesn't exist: rename DEPOSIT_PENDING → PENDING
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM StatusValues sv
                    JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                    WHERE sv.Code = 'DEPOSIT_PENDING' AND st.Code = 'RESERVATION'
                )
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM StatusValues sv
                        JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                        WHERE sv.Code = 'PENDING' AND st.Code = 'RESERVATION'
                    )
                    BEGIN
                        -- PENDING already exists: re-point reservations then delete DEPOSIT_PENDING duplicate
                        DECLARE @ExistingPendingId INT = (SELECT MIN(sv.Id) FROM StatusValues sv JOIN StatusTypes st ON sv.StatusTypeId = st.Id WHERE sv.Code = 'PENDING' AND st.Code = 'RESERVATION')
                        DECLARE @DepositPendingId  INT = (SELECT MIN(sv.Id) FROM StatusValues sv JOIN StatusTypes st ON sv.StatusTypeId = st.Id WHERE sv.Code = 'DEPOSIT_PENDING' AND st.Code = 'RESERVATION')

                        UPDATE Reservations SET ReservationStatusId = @ExistingPendingId WHERE ReservationStatusId = @DepositPendingId
                        DELETE FROM StatusValues WHERE Id = @DepositPendingId

                        UPDATE sv SET sv.IsDefault = 1
                        FROM StatusValues sv WHERE sv.Id = @ExistingPendingId
                    END
                    ELSE
                    BEGIN
                        -- PENDING doesn't exist: rename DEPOSIT_PENDING
                        UPDATE sv SET sv.Code = 'PENDING', sv.Name = 'Pending', sv.IsDefault = 1
                        FROM StatusValues sv
                        JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                        WHERE sv.Code = 'DEPOSIT_PENDING' AND st.Code = 'RESERVATION'
                    END
                END
            ");

            // Fix IsDefault: only PENDING is default
            migrationBuilder.Sql(@"
                UPDATE sv SET sv.IsDefault = 0
                FROM StatusValues sv
                JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                WHERE st.Code = 'RESERVATION' AND sv.Code != 'PENDING'
            ");

            // Migrate reservations using any non-allowed status:
            //   NO_SHOW, ABSENT, MISSED, EXPIRED → CANCELLED
            //   SEATED, ACTIVE, IN_PROGRESS, CHECKIN, CHECKED_IN → CONFIRMED
            //   WAITING, PENDING_PAYMENT, UNPAID → PENDING
            //   everything else unknown → CANCELLED
            migrationBuilder.Sql(@"
                DECLARE @ResPendingId    INT = (SELECT MIN(sv.Id) FROM StatusValues sv JOIN StatusTypes st ON sv.StatusTypeId = st.Id WHERE sv.Code = 'PENDING'    AND st.Code = 'RESERVATION')
                DECLARE @ResConfirmedId  INT = (SELECT MIN(sv.Id) FROM StatusValues sv JOIN StatusTypes st ON sv.StatusTypeId = st.Id WHERE sv.Code = 'CONFIRMED'  AND st.Code = 'RESERVATION')
                DECLARE @ResCompletedId  INT = (SELECT MIN(sv.Id) FROM StatusValues sv JOIN StatusTypes st ON sv.StatusTypeId = st.Id WHERE sv.Code = 'COMPLETED'  AND st.Code = 'RESERVATION')
                DECLARE @ResCancelledId  INT = (SELECT MIN(sv.Id) FROM StatusValues sv JOIN StatusTypes st ON sv.StatusTypeId = st.Id WHERE sv.Code = 'CANCELLED'  AND st.Code = 'RESERVATION')

                -- Migrate reservations pointing to non-allowed statuses
                UPDATE r SET r.ReservationStatusId =
                    CASE
                        WHEN sv.Code IN ('NO_SHOW', 'ABSENT', 'MISSED', 'EXPIRED', 'REJECTED')
                            THEN @ResCancelledId
                        WHEN sv.Code IN ('SEATED', 'ACTIVE', 'IN_PROGRESS', 'CHECKIN', 'CHECKED_IN')
                            THEN @ResConfirmedId
                        WHEN sv.Code IN ('WAITING', 'PENDING_PAYMENT', 'UNPAID', 'DEPOSIT_PENDING')
                            THEN @ResPendingId
                        ELSE @ResCancelledId
                    END
                FROM Reservations r
                JOIN StatusValues sv ON r.ReservationStatusId = sv.Id
                JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                WHERE st.Code = 'RESERVATION'
                  AND sv.Code NOT IN ('PENDING', 'CONFIRMED', 'COMPLETED', 'CANCELLED')

                -- Delete all non-allowed StatusValues for RESERVATION
                DELETE sv FROM StatusValues sv
                JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                WHERE st.Code = 'RESERVATION'
                  AND sv.Code NOT IN ('PENDING', 'CONFIRMED', 'COMPLETED', 'CANCELLED')
            ");

            // ════════════════════════════════════════════════════════════════════════════
            // ORDER-DETAIL (StatusValues table)
            // Allowed: PREPARING, SERVED, CANCELLED
            // Strategy for unknown statuses:
            //   - anything "pending / waiting / queued" → PREPARING
            //   - anything "ready / done / finished" → SERVED
            //   - everything else unknown → PREPARING
            // ════════════════════════════════════════════════════════════════════════════

            // Set PREPARING as default
            migrationBuilder.Sql(@"
                UPDATE sv SET sv.IsDefault = 0
                FROM StatusValues sv
                JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                WHERE st.Code = 'ORDER-DETAIL'

                UPDATE sv SET sv.IsDefault = 1
                FROM StatusValues sv
                JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                WHERE sv.Code = 'PREPARING' AND st.Code = 'ORDER-DETAIL'
            ");

            // Migrate order details pointing to non-allowed statuses
            migrationBuilder.Sql(@"
                DECLARE @PreparingId INT = (SELECT MIN(sv.Id) FROM StatusValues sv JOIN StatusTypes st ON sv.StatusTypeId = st.Id WHERE sv.Code = 'PREPARING' AND st.Code = 'ORDER-DETAIL')
                DECLARE @ServedId    INT = (SELECT MIN(sv.Id) FROM StatusValues sv JOIN StatusTypes st ON sv.StatusTypeId = st.Id WHERE sv.Code = 'SERVED'    AND st.Code = 'ORDER-DETAIL')
                DECLARE @OdCancelledId INT = (SELECT MIN(sv.Id) FROM StatusValues sv JOIN StatusTypes st ON sv.StatusTypeId = st.Id WHERE sv.Code = 'CANCELLED' AND st.Code = 'ORDER-DETAIL')

                UPDATE od SET od.ItemStatusId =
                    CASE
                        WHEN sv.Code IN ('PENDING', 'WAITING', 'QUEUED', 'NEW')
                            THEN @PreparingId
                        WHEN sv.Code IN ('READY', 'DONE', 'FINISHED', 'COMPLETED')
                            THEN @ServedId
                        WHEN sv.Code IN ('REJECTED', 'REMOVED')
                            THEN @OdCancelledId
                        ELSE @PreparingId
                    END
                FROM OrderDetails od
                JOIN StatusValues sv ON od.ItemStatusId = sv.Id
                JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                WHERE st.Code = 'ORDER-DETAIL'
                  AND sv.Code NOT IN ('PREPARING', 'SERVED', 'CANCELLED')

                -- Delete all non-allowed StatusValues for ORDER-DETAIL
                DELETE sv FROM StatusValues sv
                JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                WHERE st.Code = 'ORDER-DETAIL'
                  AND sv.Code NOT IN ('PREPARING', 'SERVED', 'CANCELLED')
            ");

            // ════════════════════════════════════════════════════════════════════════════
            // RESERVATION: Remove COMPLETED status
            // Completed state is now derived from CheckedInAt + TableSessions.EndedAt
            // Migrate existing COMPLETED reservations → CONFIRMED
            // ════════════════════════════════════════════════════════════════════════════

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM StatusValues sv
                    JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                    WHERE sv.Code = 'COMPLETED' AND st.Code = 'RESERVATION'
                )
                BEGIN
                    DECLARE @ResCompletedId INT = (
                        SELECT MIN(sv.Id) FROM StatusValues sv
                        JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                        WHERE sv.Code = 'COMPLETED' AND st.Code = 'RESERVATION'
                    )
                    DECLARE @ResConfirmedId2 INT = (
                        SELECT MIN(sv.Id) FROM StatusValues sv
                        JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                        WHERE sv.Code = 'CONFIRMED' AND st.Code = 'RESERVATION'
                    )
                    IF @ResCompletedId IS NOT NULL AND @ResConfirmedId2 IS NOT NULL
                    BEGIN
                        UPDATE Reservations
                        SET ReservationStatusId = @ResConfirmedId2
                        WHERE ReservationStatusId = @ResCompletedId

                        DELETE FROM StatusValues WHERE Id = @ResCompletedId
                    END
                END
            ");

            // ════════════════════════════════════════════════════════════════════════════
            // CLEANUP: Remove StatusTypes ORDER, TABLE, PAYMENT and all their values
            // (these use C# enums — DB rows no longer needed)
            // ════════════════════════════════════════════════════════════════════════════

            migrationBuilder.Sql(@"
                DELETE sv FROM StatusValues sv
                JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                WHERE st.Code IN ('ORDER', 'TABLE', 'PAYMENT')

                DELETE FROM StatusTypes WHERE Code IN ('ORDER', 'TABLE', 'PAYMENT')
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ─── ORDER — reverse ─────────────────────────────────────────────────────
            migrationBuilder.Sql("UPDATE Orders SET OrderStatusId = -1 WHERE OrderStatusId IN (1, 2)");
            migrationBuilder.Sql("UPDATE Orders SET OrderStatusId = 4 WHERE OrderStatusId = -1 AND OrderStatusId = 2");
            migrationBuilder.Sql("UPDATE Orders SET OrderStatusId = 3 WHERE OrderStatusId = 1");
            migrationBuilder.Sql("UPDATE Orders SET OrderStatusId = 4 WHERE OrderStatusId = 2");
            // Open(0) → Pending(0): same int, no-op

            // ─── TABLE — reverse ─────────────────────────────────────────────────────
            migrationBuilder.Sql("UPDATE Tables SET TableStatusId = 2 WHERE TableStatusId = 1");
            // Reserved data is lost — cannot fully restore

            // ─── RESERVATION — reverse ───────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM StatusValues sv
                    JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                    WHERE sv.Code = 'PENDING' AND st.Code = 'RESERVATION'
                )
                BEGIN
                    UPDATE sv SET sv.Code = 'DEPOSIT_PENDING', sv.Name = 'Deposit Pending', sv.IsDefault = 0
                    FROM StatusValues sv
                    JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                    WHERE sv.Code = 'PENDING' AND st.Code = 'RESERVATION'
                END
            ");
            // NO_SHOW, SEATED and other deleted rows cannot be restored

            // ─── ORDER-DETAIL — reverse ──────────────────────────────────────────────
            // PENDING and READY rows are deleted — cannot fully restore
        }
    }
}
