using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    /// <summary>
    /// Handles total stored ice cream count and capacity.
    /// Pure logic, no Unity lifecycle.
    /// </summary>
    [System.Serializable]
    public class IceCreamStorageSystem
    {
        [SerializeField] private int machineStored;
        [SerializeField] private int machineCapacity;

        public int MachineStored => machineStored;
        public int MachineCapacity => machineCapacity;

        public IceCreamStorageSystem(int startCapacity)
        {
            machineCapacity = startCapacity;
            machineStored = 0;
        }

        public void SetCapacity(int capacity)
        {
            machineCapacity = Mathf.Max(0, capacity);
            machineStored = Mathf.Min(machineStored, machineCapacity);
        }

        public int AddToMachine(int amount)
        {
            if (amount <= 0) return 0;

            int freeSpace = machineCapacity - machineStored;
            int add = Mathf.Clamp(amount, 0, freeSpace);
            machineStored += add;
            return add;
        }

        public bool ConsumeFromMachine(int amount)
        {
            if (amount <= 0) return false;
            if (machineStored < amount) return false;
            machineStored -= amount;
            return true;
        }

        public void SetMachineStored(int value)
        {
            machineStored = Mathf.Clamp(value, 0, machineCapacity);
        }
    }
}