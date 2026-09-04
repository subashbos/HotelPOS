using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class ReservationViewModelTests
    {
        private readonly Mock<IReservationService> _mockReservationService = new();
        private readonly Mock<ITableService> _mockTableService = new();
        private readonly Mock<ICustomerService> _mockCustomerService = new();
        private readonly Mock<INotificationService> _mockNotif = new();
        private readonly ReservationViewModel _vm;

        public ReservationViewModelTests()
        {
            _mockTableService.Setup(s => s.GetTablesAsync()).ReturnsAsync(new List<Table>());
            _mockCustomerService.Setup(s => s.GetCustomersAsync(It.IsAny<bool>())).ReturnsAsync(new List<Customer>());
            _mockReservationService.Setup(s => s.GetReservationsAsync(It.IsAny<DateTime?>())).ReturnsAsync(new List<Reservation>());

            _vm = new ReservationViewModel(
                _mockReservationService.Object,
                _mockTableService.Object,
                _mockCustomerService.Object,
                _mockNotif.Object
            );
        }

        [Fact]
        public async Task LoadDataAsync_LoadsOnlyActiveNonDeletedTables()
        {
            var tables = new List<Table>
            {
                new() { Id = 1, Name = "T1", Capacity = 4, IsActive = true, IsDeleted = false },
                new() { Id = 2, Name = "T2", Capacity = 4, IsActive = false, IsDeleted = false },
                new() { Id = 3, Name = "T3", Capacity = 4, IsActive = true, IsDeleted = true }
            };
            _mockTableService.Setup(s => s.GetTablesAsync()).ReturnsAsync(tables);

            await _vm.LoadDataAsync();

            Assert.Single(_vm.Tables);
            Assert.Equal("T1", _vm.Tables[0].Name);
        }

        [Fact]
        public async Task LoadDataAsync_LoadsCustomersAndReservations()
        {
            var customers = new List<Customer> { new() { Id = 1, Name = "Cust1" } };
            var reservations = new List<Reservation> { new() { Id = 1, TableId = 1 } };
            _mockCustomerService.Setup(s => s.GetCustomersAsync(It.IsAny<bool>())).ReturnsAsync(customers);
            _mockReservationService.Setup(s => s.GetReservationsAsync(It.IsAny<DateTime?>())).ReturnsAsync(reservations);

            await _vm.LoadDataAsync();

            Assert.Single(_vm.Customers);
            Assert.Single(_vm.Reservations);
        }

        [Fact]
        public async Task SaveReservationCommand_NoTableSelected_ShowsWarning()
        {
            await _vm.InitializationTask;
            _vm.FormTable = null;

            await _vm.SaveReservationCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.IsAny<string>()), Times.Once);
            _mockReservationService.Verify(s => s.SaveReservationAsync(It.IsAny<Reservation>()), Times.Never);
        }

        [Fact]
        public async Task SaveReservationCommand_InvalidTimeText_ShowsWarning()
        {
            await _vm.InitializationTask;
            _vm.FormTable = new Table { Id = 1, Name = "T1", Capacity = 4 };
            _vm.FormPartySize = 2;
            _vm.FormStartTimeText = "not-a-time";
            _vm.FormEndTimeText = "20:00";

            await _vm.SaveReservationCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.IsAny<string>()), Times.Once);
            _mockReservationService.Verify(s => s.SaveReservationAsync(It.IsAny<Reservation>()), Times.Never);
        }

        [Fact]
        public async Task SaveReservationCommand_EndBeforeStart_ShowsWarning()
        {
            await _vm.InitializationTask;
            _vm.FormTable = new Table { Id = 1, Name = "T1", Capacity = 4 };
            _vm.FormPartySize = 2;
            _vm.FormStartTimeText = "20:00";
            _vm.FormEndTimeText = "19:00";

            await _vm.SaveReservationCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.IsAny<string>()), Times.Once);
            _mockReservationService.Verify(s => s.SaveReservationAsync(It.IsAny<Reservation>()), Times.Never);
        }

        [Fact]
        public async Task SaveReservationCommand_Valid_SavesWithParsedTimesAndRefreshes()
        {
            await _vm.InitializationTask;
            _vm.FormTable = new Table { Id = 1, Name = "T1", Capacity = 4 };
            _vm.FormPartySize = 2;
            _vm.FormStartTimeText = "19:00";
            _vm.FormEndTimeText = "20:30";

            await _vm.SaveReservationCommand.ExecuteAsync(null);

            _mockReservationService.Verify(s => s.SaveReservationAsync(It.Is<Reservation>(r =>
                r.TableId == 1 && r.PartySize == 2 &&
                r.StartTime == TimeSpan.FromHours(19) && r.EndTime == TimeSpan.FromHours(20.5))), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void SelectingDate_TriggersReloadForNewDate()
        {
            var newDate = DateTime.Today.AddDays(1);
            _mockReservationService.Setup(s => s.GetReservationsAsync(newDate)).ReturnsAsync(new List<Reservation>
            {
                new() { Id = 42, TableId = 1 }
            });

            _vm.SelectedDate = newDate;

            Assert.Single(_vm.Reservations);
            Assert.Equal(42, _vm.Reservations[0].Id);
        }

        [Fact]
        public async Task MarkCheckedInCommand_CallsServiceAndRefreshes()
        {
            await _vm.InitializationTask;
            var reservation = new Reservation { Id = 5, Status = ReservationStatuses.Reserved };

            await _vm.MarkCheckedInCommand.ExecuteAsync(reservation);

            _mockReservationService.Verify(s => s.ChangeStatusAsync(5, ReservationStatuses.CheckedIn), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteCommand_NullReservation_DoesNothing()
        {
            await _vm.InitializationTask;

            await _vm.DeleteCommand.ExecuteAsync(null);

            _mockReservationService.Verify(s => s.DeleteReservationAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteCommand_ValidReservation_CallsServiceAndRefreshes()
        {
            await _vm.InitializationTask;
            var reservation = new Reservation { Id = 9 };

            await _vm.DeleteCommand.ExecuteAsync(reservation);

            _mockReservationService.Verify(s => s.DeleteReservationAsync(9), Times.Once);
        }

        [Fact]
        public async Task DeleteCommand_ClearsSelectedReservation()
        {
            await _vm.InitializationTask;
            var reservation = new Reservation { Id = 9 };
            _vm.SelectBlockCommand.Execute(reservation);

            await _vm.DeleteCommand.ExecuteAsync(reservation);

            Assert.Null(_vm.SelectedReservation);
        }

        [Fact]
        public async Task MarkCheckedInCommand_ClearsSelectedReservation()
        {
            await _vm.InitializationTask;
            var reservation = new Reservation { Id = 5, Status = ReservationStatuses.Reserved };
            _vm.SelectBlockCommand.Execute(reservation);

            await _vm.MarkCheckedInCommand.ExecuteAsync(reservation);

            Assert.Null(_vm.SelectedReservation);
        }

        [Fact]
        public async Task RecomputeScheduler_DefaultRange_BuildsHourMarksFrom9amTo11pm()
        {
            await _vm.InitializationTask;

            Assert.Equal(9 * 60, _vm.RangeStartMinutes);
            Assert.Equal(23 * 60, _vm.RangeEndMinutes);
            Assert.Equal(15, _vm.HourMarks.Count);
            Assert.Equal("09:00", _vm.HourMarks[0].Label);
            Assert.Equal("23:00", _vm.HourMarks[^1].Label);
            Assert.Equal(0, _vm.HourMarks[0].Left, precision: 2);
            Assert.Equal(ReservationViewModel.PxPerHour, _vm.HourMarks[1].Left, precision: 2);
            Assert.Equal(14 * ReservationViewModel.PxPerHour, _vm.TimelineWidth, precision: 2);
        }

        [Fact]
        public async Task RecomputeScheduler_ReservationOutsideDefaultRange_WidensRange()
        {
            var tables = new List<Table> { new() { Id = 1, Name = "T1", Capacity = 4, IsActive = true } };
            _mockTableService.Setup(s => s.GetTablesAsync()).ReturnsAsync(tables);
            var reservations = new List<Reservation>
            {
                new() { Id = 1, TableId = 1, StartTime = TimeSpan.FromHours(7), EndTime = TimeSpan.FromHours(8) }
            };
            _mockReservationService.Setup(s => s.GetReservationsAsync(It.IsAny<DateTime?>())).ReturnsAsync(reservations);

            await _vm.LoadDataAsync();

            Assert.Equal(7 * 60, _vm.RangeStartMinutes);
            Assert.Single(_vm.SchedulerBlocks);
        }

        [Fact]
        public async Task RecomputeScheduler_BuildsBlockPositionedByTableRowAndTime()
        {
            var tables = new List<Table>
            {
                new() { Id = 1, Name = "T1", Capacity = 4, IsActive = true },
                new() { Id = 2, Name = "T2", Capacity = 4, IsActive = true }
            };
            _mockTableService.Setup(s => s.GetTablesAsync()).ReturnsAsync(tables);
            var reservations = new List<Reservation>
            {
                new() { Id = 1, TableId = 2, StartTime = TimeSpan.FromHours(10), EndTime = TimeSpan.FromHours(11) }
            };
            _mockReservationService.Setup(s => s.GetReservationsAsync(It.IsAny<DateTime?>())).ReturnsAsync(reservations);

            await _vm.LoadDataAsync();

            var block = Assert.Single(_vm.SchedulerBlocks);
            Assert.Equal(1 * ReservationViewModel.RowHeight + 4, block.Top, precision: 2);
            Assert.Equal((10 * 60 - 9 * 60) / 60.0 * ReservationViewModel.PxPerHour, block.Left, precision: 2);
            Assert.Equal(ReservationViewModel.PxPerHour, block.Width, precision: 2);
            Assert.Equal(ReservationViewModel.RowHeight - 8, block.Height, precision: 2);
            Assert.Equal(2 * ReservationViewModel.RowHeight, _vm.TimelineHeight, precision: 2);
            Assert.Equal(reservations[0], block.Reservation);
            Assert.Equal("10:00 Walk-in", block.Label);
        }

        [Fact]
        public async Task RecomputeScheduler_BlockBrush_DiffersByStatus()
        {
            var tables = new List<Table> { new() { Id = 1, Name = "T1", Capacity = 4, IsActive = true } };
            _mockTableService.Setup(s => s.GetTablesAsync()).ReturnsAsync(tables);
            var reservations = new List<Reservation>
            {
                new() { Id = 1, TableId = 1, Status = ReservationStatuses.Reserved, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10) },
                new() { Id = 2, TableId = 1, Status = ReservationStatuses.CheckedIn, StartTime = TimeSpan.FromHours(10), EndTime = TimeSpan.FromHours(11) },
                new() { Id = 3, TableId = 1, Status = ReservationStatuses.Completed, StartTime = TimeSpan.FromHours(11), EndTime = TimeSpan.FromHours(12) },
                new() { Id = 4, TableId = 1, Status = ReservationStatuses.Cancelled, StartTime = TimeSpan.FromHours(12), EndTime = TimeSpan.FromHours(13) },
                new() { Id = 5, TableId = 1, Status = ReservationStatuses.NoShow, StartTime = TimeSpan.FromHours(13), EndTime = TimeSpan.FromHours(14) }
            };
            _mockReservationService.Setup(s => s.GetReservationsAsync(It.IsAny<DateTime?>())).ReturnsAsync(reservations);

            await _vm.LoadDataAsync();

            Assert.Equal(5, _vm.SchedulerBlocks.Count);
            Assert.NotEqual(_vm.SchedulerBlocks[0].Brush, _vm.SchedulerBlocks[1].Brush);
            Assert.NotEqual(_vm.SchedulerBlocks[1].Brush, _vm.SchedulerBlocks[2].Brush);
            Assert.NotEqual(_vm.SchedulerBlocks[2].Brush, _vm.SchedulerBlocks[3].Brush);
            Assert.Equal(_vm.SchedulerBlocks[3].Brush, _vm.SchedulerBlocks[4].Brush);
        }

        [Fact]
        public void SelectingFutureDate_ShowNowLineIsFalse()
        {
            _vm.SelectedDate = DateTime.Today.AddDays(2);

            Assert.False(_vm.ShowNowLine);
        }

        [Fact]
        public void SelectingDate_ClearsSelectedReservation()
        {
            _vm.SelectBlockCommand.Execute(new Reservation { Id = 1 });

            _vm.SelectedDate = DateTime.Today.AddDays(3);

            Assert.Null(_vm.SelectedReservation);
        }

        [Fact]
        public void SelectBlockCommand_SetsSelectedReservationAndSummary()
        {
            var reservation = new Reservation
            {
                Id = 3,
                CustomerName = "Alice",
                Status = ReservationStatuses.Reserved,
                Table = new Table { Id = 1, Name = "T5", Capacity = 4 }
            };

            _vm.SelectBlockCommand.Execute(reservation);

            Assert.Equal(reservation, _vm.SelectedReservation);
            Assert.Contains("Alice", _vm.SelectedReservationSummary);
        }

        [Fact]
        public void SelectBlockCommand_Null_ResetsSummary()
        {
            _vm.SelectBlockCommand.Execute(new Reservation { Id = 1 });

            _vm.SelectBlockCommand.Execute(null);

            Assert.Null(_vm.SelectedReservation);
            Assert.Equal("(click a reservation block above)", _vm.SelectedReservationSummary);
        }

        [Fact]
        public void ShowListAndSchedulerViewCommands_ToggleIsSchedulerView()
        {
            Assert.True(_vm.IsSchedulerView);

            _vm.ShowListViewCommand.Execute(null);
            Assert.False(_vm.IsSchedulerView);

            _vm.ShowSchedulerViewCommand.Execute(null);
            Assert.True(_vm.IsSchedulerView);
        }

        [Fact]
        public async Task OpenFormAt_PrefillsTableAndSnappedTimes()
        {
            await _vm.InitializationTask;
            var table = new Table { Id = 7, Name = "T7", Capacity = 2 };

            _vm.OpenFormAt(table, 10 * 60 + 30);

            Assert.Equal(table, _vm.FormTable);
            Assert.Equal("10:30", _vm.FormStartTimeText);
            Assert.Equal("11:30", _vm.FormEndTimeText);
            Assert.Equal(_vm.SelectedDate, _vm.FormDate);
            Assert.Equal(2, _vm.FormPartySize);
        }

        [Fact]
        public async Task OpenFormAt_NullTable_Throws()
        {
            await _vm.InitializationTask;

            Assert.Throws<ArgumentNullException>(() => _vm.OpenFormAt(null!, 10 * 60));
        }

        [Fact]
        public async Task OpenFormAt_CapsEndTimeAtRangeEnd()
        {
            await _vm.InitializationTask;
            var table = new Table { Id = 7, Name = "T7", Capacity = 2 };

            _vm.OpenFormAt(table, 22 * 60 + 45);

            Assert.Equal("23:00", _vm.FormEndTimeText);
        }
    }
}
