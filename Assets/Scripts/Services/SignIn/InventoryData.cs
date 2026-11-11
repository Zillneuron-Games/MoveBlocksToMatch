using System;

namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>
    /// Inventory is a separate data model and save record. A plain hint balance
    /// uses last-change-wins; it cannot exactly reconcile simultaneous offline spends.
    /// </summary>
    [Serializable]
    public sealed class InventoryData
    {
        public int HintsCount;
        public long HintsChangedAtUtcTicks;

        public static InventoryData Normalize(InventoryData source)
        {
            if (source == null)
            {
                return new InventoryData();
            }

            if (source.HintsCount < 0 || source.HintsChangedAtUtcTicks < 0)
            {
                throw new FormatException("Invalid inventory data.");
            }

            return new InventoryData
            {
                HintsCount = source.HintsCount,
                HintsChangedAtUtcTicks = source.HintsChangedAtUtcTicks
            };
        }

        public static bool Same(InventoryData left, InventoryData right)
        {
            InventoryData a = Normalize(left);
            InventoryData b = Normalize(right);
            return a.HintsCount == b.HintsCount && a.HintsChangedAtUtcTicks == b.HintsChangedAtUtcTicks;
        }

        public static bool IsDefault(InventoryData value)
        {
            return Same(value, new InventoryData());
        }

        public static InventoryData Merge(InventoryData left, InventoryData right)
        {
            InventoryData a = Normalize(left);
            InventoryData b = Normalize(right);
            return b.HintsChangedAtUtcTicks > a.HintsChangedAtUtcTicks || (b.HintsChangedAtUtcTicks == a.HintsChangedAtUtcTicks && b.HintsCount > a.HintsCount) ? b : a;
        }

        public static long NextTimestamp(long previousTicks)
        {
            return Math.Max(DateTime.UtcNow.Ticks, checked(previousTicks + 1));
        }
    }
}
