using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public abstract class BaseGuildListGump<T> : BaseGuildGump
    {
        private const int itemsPerPage = 8;

        private readonly InfoField<T>[] _fields;
        private readonly List<T> _originalList;
        private List<T> _displayList;
        private IComparer<T> _comparer;
        private bool _ascending;
        private string _filter;
        private int _startNumber;

        protected BaseGuildListGump(
            PlayerMobile pm, Guild g, List<T> list, IComparer<T> currentComparer, bool ascending,
            string filter, int startNumber, InfoField<T>[] fields
        )
            : base(pm, g)
        {
            _filter = filter.Trim();
            _comparer = currentComparer;
            _fields = fields;
            _ascending = ascending;
            _startNumber = startNumber;
            _originalList = list;
        }

        public virtual bool WillFilter => _filter.Length > 0;

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            // Build the filtered/sorted display list per-render so refreshes pick up
            // the latest filter/sort/pagination state.
            if (WillFilter)
            {
                var filtered = new List<T>(_originalList.Count);
                for (var i = 0; i < _originalList.Count; i++)
                {
                    if (!IsFiltered(_originalList[i], _filter))
                    {
                        filtered.Add(_originalList[i]);
                    }
                }

                _displayList = filtered;
            }
            else
            {
                _displayList = new List<T>(_originalList);
            }

            _displayList.Sort(_comparer);
            _startNumber = Math.Max(Math.Min(_startNumber, _displayList.Count - 1), 0);

            builder.AddBackground(130, 75, 385, 30, 0xBB8);
            builder.AddTextEntry(135, 80, 375, 30, 0x481, 1, _filter);
            builder.AddButton(520, 75, 0x867, 0x868, 5); // Filter Button

            var width = 0;
            for (var i = 0; i < _fields.Length; i++)
            {
                var f = _fields[i];

                builder.AddImageTiled(65 + width, 110, f.Width + 10, 26, 0xA40);
                builder.AddImageTiled(67 + width, 112, f.Width + 6, 22, 0xBBC);
                AddHtmlText(ref builder, 70 + width, 113, f.Width, 20, f.Name, false, false);

                var isComparer = _fields[i].Comparer.GetType() == _comparer.GetType();

                var buttonId = isComparer ? _ascending ? 0x983 : 0x985 : 0x2716;

                builder.AddButton(59 + width + f.Width, 117, buttonId, buttonId + (isComparer ? 1 : 0), 100 + i);

                width += f.Width + 12;
            }

            if (_startNumber <= 0)
            {
                builder.AddButton(65, 80, 0x15E3, 0x15E7, 0, GumpButtonType.Page);
            }
            else
            {
                builder.AddButton(65, 80, 0x15E3, 0x15E7, 6); // Back
            }

            if (_startNumber + itemsPerPage > _displayList.Count)
            {
                builder.AddButton(95, 80, 0x15E1, 0x15E5, 0, GumpButtonType.Page);
            }
            else
            {
                builder.AddButton(95, 80, 0x15E1, 0x15E5, 7); // Forward
            }

            var itemNumber = 0;

            if (_ascending)
            {
                for (var i = _startNumber; i < _startNumber + itemsPerPage && i < _displayList.Count; i++)
                {
                    DrawEntry(ref builder, _displayList[i], i, itemNumber++);
                }
            }
            else // descending, go from bottom of list to the top
            {
                for (var i = _displayList.Count - 1 - _startNumber;
                     i >= 0 && i >= _displayList.Count - itemsPerPage - _startNumber;
                     i--)
                {
                    DrawEntry(ref builder, _displayList[i], i, itemNumber++);
                }
            }

            DrawEndingEntry(ref builder, itemNumber);

            BuildListExtras(ref builder);
        }

        protected virtual void BuildListExtras(ref DynamicGumpBuilder builder)
        {
        }

        protected virtual void DrawEndingEntry(ref DynamicGumpBuilder builder, int itemNumber)
        {
        }

        public virtual bool HasRelationship(T o) => false;

        protected virtual void DrawEntry(ref DynamicGumpBuilder builder, T o, int index, int itemNumber)
        {
            var width = 0;
            var values = GetValuesFor(o, _fields.Length);
            for (var j = 0; j < _fields.Length; j++)
            {
                var f = _fields[j];

                builder.AddImageTiled(65 + width, 138 + itemNumber * 28, f.Width + 10, 26, 0xA40);
                builder.AddImageTiled(67 + width, 140 + itemNumber * 28, f.Width + 6, 22, 0xBBC);
                AddHtmlText(
                    ref builder,
                    70 + width,
                    141 + itemNumber * 28,
                    f.Width,
                    20,
                    values[j],
                    false,
                    false
                );

                width += f.Width + 12;
            }

            if (HasRelationship(o))
            {
                builder.AddButton(40, 143 + itemNumber * 28, 0x8AF, 0x8AF, 200 + index); // Info Button
            }
            else
            {
                builder.AddButton(40, 143 + itemNumber * 28, 0x4B9, 0x4BA, 200 + index); // Info Button
            }
        }

        protected abstract TextDefinition[] GetValuesFor(T o, int aryLength);

        protected abstract bool IsFiltered(T o, string filter);

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            base.OnResponse(sender, info);

            if (sender.Mobile is not PlayerMobile pm || !IsMember(pm, guild))
            {
                return;
            }

            var id = info.ButtonID;

            switch (id)
            {
                case 5: // Filter
                    {
                        var t = info.GetTextEntry(1);
                        _filter = (t ?? "").Trim();
                        _startNumber = 0;
                        pm.SendGump(this);
                        return;
                    }
                case 6: // Back
                    {
                        _startNumber -= itemsPerPage;
                        pm.SendGump(this);
                        return;
                    }
                case 7: // Forward
                    {
                        _startNumber += itemsPerPage;
                        pm.SendGump(this);
                        return;
                    }
            }

            if (id >= 100 && id < 100 + _fields.Length)
            {
                var comparer = _fields[id - 100].Comparer;

                if (_comparer.GetType() == comparer.GetType())
                {
                    _ascending = !_ascending;
                }
                else
                {
                    _comparer = comparer;
                }

                _startNumber = 0;
                pm.SendGump(this);
            }
            else if (id >= 200)
            {
                // The display list is rebuilt every render, so we use the most recent
                // build to resolve the clicked entry.
                var list = _displayList ?? _originalList;
                var idx = id - 200;
                if (idx >= 0 && idx < list.Count)
                {
                    pm.SendGump(GetObjectInfoGump(player, guild, list[idx]));
                }
            }
        }

        public abstract BaseGump GetObjectInfoGump(PlayerMobile pm, Guild g, T o);

        public void ResendGump()
        {
            player.SendGump(this);
        }
    }

    public struct InfoField<T>
    {
        public TextDefinition Name { get; }

        public int Width { get; }

        public IComparer<T> Comparer { get; }

        public InfoField(TextDefinition name, int width, IComparer<T> comparer)
        {
            Name = name;
            Width = width;
            Comparer = comparer;
        }
    }
}
