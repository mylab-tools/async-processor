using System;

namespace MyLab.AsyncProcessor.Api
{
    public class UnsupportedMediaTypeException : NotSupportedException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnsupportedMediaTypeException"/>
        /// </summary>
        public UnsupportedMediaTypeException(string mediaType) :base("Unsupported Media Type: '" + mediaType + "'")
        {
            
        }
    }
}
