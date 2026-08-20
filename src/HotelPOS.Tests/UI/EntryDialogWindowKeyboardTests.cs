using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HotelPOS.ViewModels;
using HotelPOS.Views.Common;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class EntryDialogWindowKeyboardTests
    {
        // Minimal, XAML-free subclass so the window can be constructed normally (no
        // InitializeComponent / Application.Current resource resolution needed), while
        // still going through the real EntryDialogWindow wiring under test.
        private class TestEntryDialogWindow : EntryDialogWindow
        {
            public TestEntryDialogWindow(IEntryDialogViewModel viewModel)
            {
                InitializeEntryDialog(viewModel);
            }
        }

        private class DummyPresentationSource : PresentationSource
        {
            public override Visual RootVisual { get; set; } = null!;
            public override bool IsDisposed => false;
            protected override CompositionTarget GetCompositionTargetCore() => null!;
        }

        private static void RunOnSta(Action action)
        {
            Exception? threadEx = null;

            var t = new Thread(() =>
            {
                try
                {
                    action();
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

        private static void InvokeKeyDown(TestEntryDialogWindow view, Key key)
        {
            var source = new DummyPresentationSource();
            var keyEventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
            {
                RoutedEvent = Keyboard.KeyDownEvent
            };

            var handler = typeof(EntryDialogWindow).GetMethod(
                "EntryDialog_KeyDown",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(handler);
            handler!.Invoke(view, new object[] { view, keyEventArgs });
        }

        // Note: Ctrl+S is deliberately not covered here. EntryDialogWindow's Ctrl+S branch
        // checks the live OS keyboard-modifier state via Keyboard.Modifiers, which reflects
        // real hardware key state and cannot be faked from a unit test without simulated
        // input (e.g. SendInput), so it isn't reliably testable in this style of test.

        [Fact]
        public void EntryDialogWindow_EnterKey_TriggersSave()
        {
            RunOnSta(() =>
            {
                var vm = new Mock<IEntryDialogViewModel>();
                vm.Setup(v => v.SaveCommand.CanExecute(null)).Returns(true);
                var view = new TestEntryDialogWindow(vm.Object);

                InvokeKeyDown(view, Key.Enter);

                vm.Verify(v => v.SaveCommand.Execute(null), Times.Once);
            });
        }

        [Fact]
        public void EntryDialogWindow_Enter_WhenSaveCommandCannotExecute_DoesNotInvokeSave()
        {
            RunOnSta(() =>
            {
                var vm = new Mock<IEntryDialogViewModel>();
                vm.Setup(v => v.SaveCommand.CanExecute(null)).Returns(false);
                var view = new TestEntryDialogWindow(vm.Object);

                InvokeKeyDown(view, Key.Enter);

                vm.Verify(v => v.SaveCommand.Execute(null), Times.Never);
            });
        }

        [Fact]
        public void EntryDialogWindow_Enter_InMultilineTextBox_DoesNotTriggerSave()
        {
            RunOnSta(() =>
            {
                var vm = new Mock<IEntryDialogViewModel>();
                vm.Setup(v => v.SaveCommand.CanExecute(null)).Returns(true);
                var view = new TestEntryDialogWindow(vm.Object);

                // Simulate focus being inside a multi-line (AcceptsReturn) TextBox, e.g. a
                // Notes/Description field, so Enter should insert a newline instead of saving.
                var multilineBox = new TextBox { AcceptsReturn = true };
                FocusManager.SetFocusedElement(view, multilineBox);

                InvokeKeyDown(view, Key.Enter);

                vm.Verify(v => v.SaveCommand.Execute(null), Times.Never);
            });
        }
    }
}
