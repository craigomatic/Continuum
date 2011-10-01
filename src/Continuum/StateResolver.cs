using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Continuum
{
    /// <summary>
    /// Stores a mapping between the GUID of a given state controller and the ID assigned to it by the State Allocation Table
    /// </summary>
    public class StateResolver : IStateResolver
    {
        /// <summary>
        /// Gets the allocation table.
        /// </summary>
        public IDictionary<Guid, short> AllocationTable
        {
            get
            {
                lock (_Lock)
                {
                    return _AllocationTable;
                }
            }
        }

        private Dictionary<Guid, short> _AllocationTable;
        private Dictionary<Guid, IStateController> _Controllers;
        private object _Lock;
        private short _CurrentId;

        public StateResolver()
        {
            _AllocationTable = new Dictionary<Guid, short>();
            _Controllers = new Dictionary<Guid, IStateController>();
            _Lock = new object();
            _CurrentId = 0;
        }

        /// <summary>
        /// Adds the specified state controller.
        /// </summary>
        /// <param name="stateController">The state controller.</param>
        /// <returns></returns>
        public short Add(IStateController stateController)
        {
            lock (_Lock)
            {
                _Controllers.Add(stateController.Guid, stateController);
                
                var key = _CurrentId;

                _AllocationTable.Add(stateController.Guid, key);

                _CurrentId++;
                return key;
            }
        }

        /// <summary>
        /// Removes the specified state controller.
        /// </summary>
        /// <param name="stateController">The state controller.</param>
        public void Remove(IStateController stateController)
        {
            lock (_Lock)
            {
                if (_Controllers.ContainsKey(stateController.Guid))
                {
                    _Controllers.Remove(stateController.Guid);
                }

                if (_AllocationTable.ContainsKey(stateController.Guid))
                {
                    _AllocationTable.Remove(stateController.Guid);
                }
            }
        }

        /// <summary>
        /// Clears all IStateController instances
        /// </summary>
        public void Clear()
        {
            lock(_Lock)
            {
                _Controllers.Clear();
                _AllocationTable.Clear();
                _CurrentId = 0;
            }
        }

        /// <summary>
        /// Finds the IStateController instance identified by the specified GUID
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public IStateController Find(Guid guid)
        {
            lock(_Lock)
            {
                if (_Controllers.ContainsKey(guid))
                {
                    return _Controllers[guid];
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified GUID is allocated.
        /// </summary>
        /// <param name="guid">The GUID.</param>
        /// <returns>
        ///   <c>true</c> if the specified GUID is allocated; otherwise, <c>false</c>.
        /// </returns>
        public bool IsAllocated(Guid guid)
        {
            return _AllocationTable.ContainsKey(guid);
        }

        /// <summary>
        /// Gets the allocated id.
        /// </summary>
        /// <param name="guid">The GUID.</param>
        /// <returns></returns>
        public short GetAllocatedId(Guid guid)
        {
            lock (_Lock)
            {
                return _AllocationTable[guid];
            }
        }
    }
}
