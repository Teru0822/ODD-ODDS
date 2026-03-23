using System;

namespace QubicNS
{
    public static class CornerMasks
    {
        public const UInt16 None = 0x0;
        public const UInt16 Straight = 0x1;
        public const UInt16 ConvexL = 0x2;
        public const UInt16 ConcaveL = 0x4;
        public const UInt16 GenericL = 0x40;
        public const UInt16 End = 0x8;
        public const UInt16 T = 0x10;
        public const UInt16 X = 0x20;
        public const UInt16 o = 0x80;
    }

    [Serializable, Flags]
    public enum CornerType : UInt16
    {
        None = 0x0,
        Straight = CornerMasks.Straight,
        ConvexL = CornerMasks.ConvexL,
        ConcaveL = CornerMasks.ConcaveL,
        End = CornerMasks.End,
        T = CornerMasks.T,
        X = CornerMasks.X,
    }
}