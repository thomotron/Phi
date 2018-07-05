using System;

namespace PhiClient
{
    public class TypeMismatchException : Exception
    {
        public override string Message { get; }

        public Type ExpectedType;
        public Type ActualType;

        public TypeMismatchException(Type expected, Type actual)
        {
            this.ExpectedType = expected;
            this.ActualType = actual;

            this.Message = $"Expected type {ExpectedType.FullName}, instead got type {ActualType.FullName}";
        }
    }
}