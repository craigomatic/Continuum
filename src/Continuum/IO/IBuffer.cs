using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Continuum.IO
{
    /// <summary>
    /// Represents a FIFO buffer 
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the buffer</typeparam>
    public interface IBuffer<T>
    {
        /// <summary>
        /// Gets the count of elements in the buffer
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds an element to the end of the buffer
        /// </summary>
        /// <param name="obj"></param>
        void Enqueue(T obj);

        /// <summary>
        /// Attempts to dequeue the first element
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        bool TryDequeue(out T obj);

        /// <summary>
        /// Attempts to peek at the first element
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        bool TryPeek(out T obj);
    }
}
