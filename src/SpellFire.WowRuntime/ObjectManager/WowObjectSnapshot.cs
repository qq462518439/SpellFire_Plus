using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.ObjectManager
{
    public class WowObjectSnapshot
    {
        public WowObjectSnapshot(ulong guid, int entry, string name, ObjectKind kind, Vector3 position, bool isValid)
        {
            Guid = guid;
            Entry = entry;
            Name = name ?? string.Empty;
            Kind = kind;
            Position = position;
            IsValid = isValid;
        }

        public ulong Guid { get; }

        public int Entry { get; }

        public string Name { get; }

        public ObjectKind Kind { get; }

        public Vector3 Position { get; }

        public bool IsValid { get; }
    }
}
