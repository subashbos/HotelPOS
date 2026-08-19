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
    }
}
