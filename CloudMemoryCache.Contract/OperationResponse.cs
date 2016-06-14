using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudMemoryCache.Contract
{
    public class OperationResponse<T>
    {
        public OperationResponse()
        {
            Messages = new List<string>();
        }

        public List<string> Messages { get; set; }

        public string Message
        {
            get { return String.Join(" ", Messages ?? new List<string>()); }    
        }

        public T Value { get; set; }

        public bool IsSuccessful
        {
            get
            {
                if (typeof(T) == typeof(bool))
                {
                    return (bool)Convert.ChangeType(Value, typeof(bool));
                }
                if (Value == null)
                {
                    return false;
                }
                return !Value.Equals(default(T));
            }
        }

    }
}
