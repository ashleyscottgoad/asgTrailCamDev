﻿namespace Common.Cosmos
{
    [Serializable]
    public class OptimisticConcurrencyException : Exception
    {
        public OptimisticConcurrencyException()
        {
        }

        public OptimisticConcurrencyException(string message) : base(message)
        {
        }

        public OptimisticConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}