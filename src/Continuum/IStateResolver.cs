using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Continuum
{
    /// <summary>
    /// Manages IStateController instances
    /// </summary>
    public interface IStateResolver
    {
        /// <summary>
        /// Gets the allocation table.
        /// </summary>
        IDictionary<Guid, short> AllocationTable { get; }

        /// <summary>
        /// Adds the specified state controller.
        /// </summary>
        /// <param name="stateController">The state controller.</param>
        /// <returns></returns>
        short Add(IStateController stateController);

        /// <summary>
        /// Removes the specified state controller.
        /// </summary>
        /// <param name="stateController">The state controller.</param>
        void Remove(IStateController stateController);

        /// <summary>
        /// Clears all IStateController instances
        /// </summary>
        void Clear();

        /// <summary>
        /// Finds the IStateController instance identified by the specified GUID
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        IStateController Find(Guid id);

        /// <summary>
        /// Determines whether the specified GUID is allocated.
        /// </summary>
        /// <param name="guid">The GUID.</param>
        /// <returns>
        ///   <c>true</c> if the specified GUID is allocated; otherwise, <c>false</c>.
        /// </returns>
        bool IsAllocated(Guid guid);

        /// <summary>
        /// Gets the allocated id.
        /// </summary>
        /// <param name="guid">The GUID.</param>
        /// <returns></returns>
        short GetAllocatedId(Guid guid);
    }
}
