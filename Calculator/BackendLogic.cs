using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calculator
{
    enum Operation
    {
        Null,
        Plus,
        Minus,
        Multiply,
        Divide,
        Percent,
        Revert,
        Power,
        Sqrt,
        SignChange
    }

    public partial class MainWindow
    {
        private double _lastResult = double.NaN;
        private double LastResult { get => _lastResult; set => _lastResult = value; }
        private Operation _lastOperation = Operation.Null;
        private Operation LastOperation { get => _lastOperation; set => _lastOperation = value; }
        private double _lastValue = double.NaN;
        private double LastValue { get => _lastValue; set => _lastValue = value; }
        private double _memory = double.NaN;
        private double Memory { get => _memory; set => _memory = value; }



        private void ClearMemory()
        {
            Memory = double.NaN;
        }

        private double RecallMemory()
        {
            if (double.IsNaN(Memory)) throw new MemberAccessException("Memory is empty");
            return Memory;
        }

        private void ComputeMemory(Operation operation, double value)
        {
            switch (operation)
            {
                case Operation.Plus:
                    if (double.IsNaN(Memory))
                    {
                        Memory = value;
                        return;
                    }
                    Memory += value;
                    break;
                case Operation.Minus:
                    if (double.IsNaN(Memory))
                    {
                        Memory = -value;
                        return;
                    }
                    Memory -= value;
                    break;
                default:
                    throw new InvalidOperationException("Only + and - operations are allowed on memory");
            }
        }

        private void SaveLast(double lasResult = double.NaN, Operation lastOperation = Operation.Null)
        {
            if (double.IsNaN(LastResult))
                LastResult = lasResult;
            LastOperation = lastOperation;
            LastValue = double.NaN;
        }

        private void ResetBackend()
        {
            LastResult = double.NaN;
            LastOperation = Operation.Null;
            LastValue = double.NaN;
        }

        private double ComputeResult(double currentValue)
        {
            double computeValue;
            if (double.IsNaN(LastValue))
            {
                computeValue = currentValue;
                LastValue = currentValue;
            }
            else
            {
                computeValue = LastValue;
            }

            switch (LastOperation)
            {
                case Operation.Null:
                    throw new ArgumentNullException();
                case Operation.Plus:
                    LastResult += computeValue;
                    break;
                case Operation.Minus:
                    LastResult -= computeValue;
                    break;
                case Operation.Multiply:
                    LastResult *= computeValue;
                    break;
                case Operation.Divide:
                    if (computeValue == 0) throw new DivideByZeroException("Cannot divide by 0");
                    LastResult /= computeValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return LastResult;
        }

        private double ComputeResult(Operation specialOperation, double value)
        {
            switch (specialOperation)
            {
                case Operation.Percent:
                    if (LastOperation == Operation.Multiply || LastOperation == Operation.Divide) return value / 100.0;
                    return (LastResult * value) / 100.0;
                case Operation.Power:
                    return value * value;
                case Operation.Sqrt:
                    if (value < 0)
                        throw new InvalidOperationException("Invalid operation");
                    return Math.Sqrt(value);
                case Operation.SignChange:
                    return 0 - value;
                case Operation.Revert:
                    if (value == 0) throw new DivideByZeroException("Cannot divide by 0");
                    return 1.0 / value;
                default:
                    throw new InvalidEnumArgumentException($@"Given argument {specialOperation.ToString()} is not a special operation");
            }
        }
    }
}
