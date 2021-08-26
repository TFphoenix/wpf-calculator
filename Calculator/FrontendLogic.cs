using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Calculator
{
    public partial class MainWindow
    {
        private String _screenText = "0";
        public String ScreenText
        {
            get => _screenText;
            set
            {
                // When entering Error State
                if (!IsNotInErrorState) goto dataBinding;

                // Digit Grouping
                if (IsGroupSeparatorOn)
                {
                    try
                    {
                        string afterDecimalSeparator = value.Contains(decimalSeparator) ? value.Substring(value.IndexOf(decimalSeparator, StringComparison.Ordinal)) : "";
                        value = double.Parse(value).ToString("N", CultureInfo.CurrentCulture);
                        value = value.Substring(0, value.IndexOf(decimalSeparator, StringComparison.Ordinal)) + afterDecimalSeparator;
                        //if (value.Contains(".00")) value = value.Substring(0, value.Length - 3);
                        //value += afterDecimalSeparator;
                    }
                    catch (FormatException exception)
                    {
                        EnterErrorState(exception.Message);
                    }
                }

                // Data Binding
                dataBinding:
                if (_screenText != value)
                {
                    _screenText = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool shouldOverride = true;
        private bool _isNotInErrorState = true;
        public bool IsNotInErrorState
        {
            get => _isNotInErrorState;
            set
            {
                if (_isNotInErrorState != value)
                {
                    _isNotInErrorState = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _isDecimalSeparatorOn = false;
        private bool IsDecimalSeparatorOn { get => _isDecimalSeparatorOn; set => _isDecimalSeparatorOn = value; }
        private static readonly string decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        private bool _isGroupSeparatorOn = Properties.Settings.Default.IsDigitGroupingOn;
        public bool IsGroupSeparatorOn
        {
            get => _isGroupSeparatorOn;
            set
            {
                // Persistent Data
                Properties.Settings.Default.IsDigitGroupingOn = value;
                Properties.Settings.Default.Save();

                // Data Binding
                if (_isGroupSeparatorOn != value)
                {
                    _isGroupSeparatorOn = value;
                    OnPropertyChanged();
                }

                //Digit Grouping
                ScreenText = value ? ScreenText : ScreenText.Trim(groupSeparator.ToCharArray());
            }
        }
        private static readonly string groupSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
        private bool equalPressed = false;



        private void ResetScreen()
        {
            shouldOverride = true;
            IsDecimalSeparatorOn = false;
            ScreenText = "0";
        }

        private void EnterErrorState(string error = "Unknown Error")
        {
            IsNotInErrorState = false;
            ScreenText = error;
            shouldOverride = true;
            ResetBackend();
        }

        private void Digit_OnClick(object sender, RoutedEventArgs e)
        {
            var digitValue = ((Button)sender).Content.ToString();
            if (!IsNotInErrorState) IsNotInErrorState = true;

            if (digitValue == "0" && shouldOverride) return;

            if (shouldOverride)
            {
                ScreenText = digitValue;
                shouldOverride = false;
            }
            else
            {
                ScreenText += digitValue;
            }
        }

        private void Digit_OnClick(string digitValue)
        {
            if (!IsNotInErrorState) IsNotInErrorState = true;

            if (digitValue == "0" && shouldOverride) return;

            if (shouldOverride)
            {
                ScreenText = digitValue;
                shouldOverride = false;
            }
            else
            {
                ScreenText += digitValue;
            }
        }

        private void ActivateDecimalSeparator()
        {
            if (!IsDecimalSeparatorOn)
            {
                IsDecimalSeparatorOn = true;
                if (shouldOverride)
                {
                    shouldOverride = false;
                    ScreenText = "0" + decimalSeparator;
                    return;
                }
                shouldOverride = false;
                ScreenText = ScreenText += decimalSeparator;
            }
        }

        private void Operator(string operatorType)
        {
            if (equalPressed) equalPressed = false;
            else if (LastOperation != Operation.Null && operatorType != "BtnPrc" && shouldOverride == false) Equals();

            switch (operatorType)
            {
                case "BtnPls":// +
                    SaveLast(double.Parse(ScreenText), Operation.Plus);
                    break;
                case "BtnMin":// -
                    SaveLast(double.Parse(ScreenText), Operation.Minus);
                    break;
                case "BtnMlt":// *
                    SaveLast(double.Parse(ScreenText), Operation.Multiply);
                    break;
                case "BtnDiv":// /
                    SaveLast(double.Parse(ScreenText), Operation.Divide);
                    break;
                case "BtnPrc":// %
                    if (LastOperation == Operation.Null) { ResetScreen(); ResetBackend(); return; }
                    ScreenText = ComputeResult(Operation.Percent, double.Parse(ScreenText)).ToString(CultureInfo.CurrentCulture);
                    break;
                case "BtnRev":// 1/x
                    try
                    {
                        ScreenText = ComputeResult(Operation.Revert, double.Parse(ScreenText))
                            .ToString(CultureInfo.CurrentCulture);
                    }
                    catch (DivideByZeroException exception)
                    {
                        EnterErrorState(exception.Message);
                    }

                    break;
                case "BtnPow":// x^2
                    ScreenText = ComputeResult(Operation.Power, double.Parse(ScreenText)).ToString(CultureInfo.CurrentCulture);
                    break;
                case "BtnSqr":// sqrt(x)
                    try
                    {
                        ScreenText = ComputeResult(Operation.Sqrt, double.Parse(ScreenText)).ToString(CultureInfo.CurrentCulture);
                    }
                    catch (InvalidOperationException exception)
                    {
                        EnterErrorState(exception.Message);
                    }

                    break;
                default:
                    throw new InvalidOperationException($"Unknown operator sender: {operatorType}");
            }

            shouldOverride = true;
            IsDecimalSeparatorOn = false;
        }

        private void Operator_OnClick(object sender, RoutedEventArgs e)
        {
            var senderName = ((Button)sender).Name;
            Operator(senderName);
        }

        private void Memory_OnClick(object sender, RoutedEventArgs e)
        {
            var senderName = ((Button)sender).Name;

            switch (senderName)
            {
                case "BtnMc":
                    ClearMemory();
                    break;
                case "BtnMr":
                    try
                    {
                        ScreenText = RecallMemory().ToString(CultureInfo.CurrentCulture);
                    }
                    catch (MemberAccessException exception)
                    {
                        EnterErrorState(exception.Message);
                    }
                    break;
                case "BtnMp":
                    ComputeMemory(Operation.Plus, double.Parse(ScreenText));
                    break;
                case "BtnMm":
                    ComputeMemory(Operation.Minus, double.Parse(ScreenText));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown memory sender: {senderName}");
            }
        }

        private void Equals()
        {
            try
            {
                ScreenText = ComputeResult(double.Parse(ScreenText)).ToString(CultureInfo.CurrentCulture);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (DivideByZeroException exception)
            {
                EnterErrorState(exception.Message);
            }
        }

        private void Equals_OnClick(object sender, RoutedEventArgs e)
        {
            equalPressed = true;
            Equals();
        }

        private void Equals_OnKey()
        {
            equalPressed = true;
            Equals();
        }

        private void Control(string controlType)
        {
            if (!IsNotInErrorState)
            {
                ResetScreen();
                ResetBackend();
                IsNotInErrorState = true;
                return;
            }

            switch (controlType)
            {
                case "BtnCe":
                    ResetScreen();
                    LastResult = 0;
                    break;
                case "BtnC":
                    ResetScreen();
                    ResetBackend();
                    break;
                case "BtnDel":
                    if (!shouldOverride)
                    {
                        ScreenText = ScreenText.Remove(ScreenText.Length - 1);
                        if (string.IsNullOrEmpty(ScreenText))
                        {
                            ResetScreen();
                        }
                    }
                    break;
                case "BtnSgn":// +/-
                    ScreenText = ComputeResult(Operation.SignChange, double.Parse(ScreenText)).ToString(CultureInfo.CurrentCulture);
                    break;
                case "BtnDot":// .
                    ActivateDecimalSeparator();
                    break;
                default:
                    throw new InvalidOperationException($"Unknown control sender: {controlType}");
            }
        }

        private void Control_OnClick(object sender, RoutedEventArgs e)
        {
            var senderName = ((Button)sender).Name;
            Control(senderName);
        }

        private void OnCut(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ScreenText);
            ResetScreen();
        }

        private void OnCopy(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ScreenText);
        }

        private void OnPaste(object sender, RoutedEventArgs e)
        {
            var clipboardText = Clipboard.GetText();
            Regex regex = new Regex(@"^-?[0-9]+(.[0-9]+)?$");//TODO: Enrich Regex
            if (regex.IsMatch(clipboardText))
            {
                ScreenText = clipboardText;
                IsDecimalSeparatorOn = ScreenTextBox.Text.Contains(decimalSeparator);
            }
            else
            {
                EnterErrorState("Invalid input");
            }
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            // Digit
            if (e.Key == Key.D0 || e.Key == Key.D1 || e.Key == Key.D2 || e.Key == Key.D3 || e.Key == Key.D4 || e.Key == Key.D5 || e.Key == Key.D6 || e.Key == Key.D7 || e.Key == Key.D8 || e.Key == Key.D9
                || e.Key == Key.NumPad0 || e.Key == Key.NumPad1 || e.Key == Key.NumPad2 || e.Key == Key.NumPad3 || e.Key == Key.NumPad4 || e.Key == Key.NumPad5 || e.Key == Key.NumPad6 || e.Key == Key.NumPad7 || e.Key == Key.NumPad8 || e.Key == Key.NumPad9)
            {
                var keyValue = e.Key.ToString();
                Digit_OnClick(keyValue[keyValue.Length - 1].ToString());
            }
            else
            {
                switch (e.Key)
                {
                    // Control
                    case Key.Back:
                        Control("BtnDel");
                        break;
                    case Key.Delete:
                        Control("BtnC");
                        break;
                    case Key.Decimal:
                        ActivateDecimalSeparator();
                        break;
                    // Equals
                    case Key.Enter:
                        Equals_OnKey();
                        break;
                    // Operator
                    case Key.OemMinus:
                        Operator("BtnMin");
                        break;
                    case Key.Subtract:
                        Operator("BtnMin");
                        break;
                    case Key.OemPlus:
                        Operator("BtnPls");
                        break;
                    case Key.Add:
                        Operator("BtnPls");
                        break;
                    case Key.Divide:
                        Operator("BtnDiv");
                        break;
                    case Key.Multiply:
                        Operator("BtnMlt");
                        break;
                    default:
                        return;
                }
            }

            e.Handled = true;
        }
    }
}
