using System.Runtime.CompilerServices;

namespace Server;

public partial class Mobile
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.ItemAtEnumerable<Item> GetItemsAt() =>
        m_Map == null ? Map.ItemAtEnumerable<Item>.Empty : m_Map.GetItemsAt(m_Location);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.ItemAtEnumerable<T> GetItemsAt<T>() where T : Item =>
        m_Map == null ? Map.ItemAtEnumerable<T>.Empty : m_Map.GetItemsAt<T>(m_Location);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.ItemBoundsEnumerable<Item> GetItemsInRange(int range) => GetItemsInRange<Item>(range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.ItemBoundsEnumerable<T> GetItemsInRange<T>(int range) where T : Item =>
        m_Map == null ? Map.ItemBoundsEnumerable<T>.Empty : m_Map.GetItemsInRange<T>(m_Location, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.MobileAtEnumerable<Mobile> GetMobilesInRange() => GetMobilesInRange<Mobile>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.MobileAtEnumerable<T> GetMobilesInRange<T>() where T : Mobile =>
        m_Map == null ? Map.MobileAtEnumerable<T>.Empty : m_Map.GetMobilesAt<T>(m_Location);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.MobileBoundsEnumerable<Mobile> GetMobilesInRange(int range) => GetMobilesInRange<Mobile>(range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.MobileBoundsEnumerable<T> GetMobilesInRange<T>(int range) where T : Mobile =>
        m_Map == null ? Map.MobileBoundsEnumerable<T>.Empty : m_Map.GetMobilesInRange<T>(m_Location, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.ClientAtEnumerable GetClientsAt() =>
        m_Map == null ? Map.ClientAtEnumerable.Empty : Map.GetClientsAt(m_Location);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.ClientBoundsEnumerable GetClientsInRange(int range) =>
        m_Map == null ? Map.ClientBoundsEnumerable.Empty : Map.GetClientsInRange(m_Location, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.ItemDistanceEnumerable<Item> GetItemsInRangeByDistance(int range) =>
        GetItemsInRangeByDistance<Item>(range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.ItemDistanceEnumerable<T> GetItemsInRangeByDistance<T>(int range) where T : Item =>
        m_Map == null ? default : m_Map.GetItemsInRangeByDistance<T>(m_Location, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.MobileDistanceEnumerable<Mobile> GetMobilesInRangeByDistance(int range) =>
        GetMobilesInRangeByDistance<Mobile>(range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.MobileDistanceEnumerable<T> GetMobilesInRangeByDistance<T>(int range) where T : Mobile =>
        m_Map == null ? default : m_Map.GetMobilesInRangeByDistance<T>(m_Location, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map.ClientDistanceEnumerable GetClientsInRangeByDistance(int range) =>
        m_Map == null ? default : m_Map.GetClientsInRangeByDistance(m_Location, range);
}
