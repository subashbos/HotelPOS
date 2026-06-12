using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using HotelPOS.Views;
using Moq;
using Xunit;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace HotelPOS.Tests
{
    public class BillingKeyboardShortcutsTests
    {
        private class DummyPresentationSource : PresentationSource
        {
            public override Visual RootVisual { get; set; } = null!;
            public override bool IsDisposed => false;
            protected override CompositionTarget GetCompositionTargetCore() => null!;
        }

        [Fact]
        public void BillingView_F4_Key_Triggers_Checkout()
        {
            Exception? threadEx = null;

            var t = new Thread(() =>
            {
                try
                {
                    // Arrange
                    var itemService = new Mock<IItemService>();
                    var cartService = new Mock<ICartService>();
                    var orderService = new Mock<IOrderService>();
                    var settingService = new Mock<ISettingService>();
                    var categoryService = new Mock<ICategoryService>();
                    var notificationService = new Mock<INotificationService>();
                    var cashService = new Mock<ICashService>();
                    var tableService = new Mock<ITableService>();

                    cartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
                    cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1 } });
                    settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
                    cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);

                    var vm = new BillingViewModel(
                        itemService.Object,
                        cartService.Object,
                        orderService.Object,
                        settingService.Object,
                        categoryService.Object,
                        notificationService.Object,
                        cashService.Object,
                        tableService.Object);

                    vm.PaymentMode = "Cash";
                    vm.Cart.Add(new CartRow { ItemId = 1, ItemName = "Item 1", Quantity = 1 });

                    // Bypassing XAML loading and constructor using modern RuntimeHelpers to avoid Application.Current resource resolution issues
                    var view = (BillingView)RuntimeHelpers.GetUninitializedObject(typeof(BillingView));
                    
                    typeof(BillingView)
                        .GetField("_viewModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(view, vm);

                    // Construct KeyEventArgs for F4 key
                    var source = new DummyPresentationSource();
                    var keyEventArgs = new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        source,
                        0,
                        Key.F4)
                    {
                        RoutedEvent = Keyboard.PreviewKeyDownEvent
                    };

                    // Act - Invoke the private event handler directly via reflection
                    var previewKeyDownMethod = typeof(BillingView).GetMethod(
                        "UserControl_PreviewKeyDown", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    Assert.NotNull(previewKeyDownMethod);
                    previewKeyDownMethod!.Invoke(view, new object[] { view, keyEventArgs });

                    // Assert
                    cashService.Verify(s => s.GetCurrentSessionAsync(), Times.Once);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            if (threadEx != null)
            {
                throw new Exception("Exception thrown on STA thread: " + threadEx.Message, threadEx);
            }
        }
    }
}
