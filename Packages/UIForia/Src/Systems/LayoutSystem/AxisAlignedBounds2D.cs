﻿using Unity.Mathematics;

namespace UIForia.Graphics {

    public struct AxisAlignedBounds2D {

        public float xMin;
        public float yMin;
        public float xMax;
        public float yMax;

        public AxisAlignedBounds2D(float xMin, float yMin, float xMax, float yMax) {
            this.xMin = xMin;
            this.yMin = yMin;
            this.xMax = xMax;
            this.yMax = yMax;
        }

        public float Width {
            get => xMax - xMin;
        }

        public float Height {
            get => yMax - yMin;
        }

        // todo -- verify that this is correct, not sure that I want the additional - left / top
        public static AxisAlignedBounds2D Intersect(in AxisAlignedBounds2D a, in AxisAlignedBounds2D b) {
            float left = math.max(a.xMin, b.xMin);
            float width = math.min(a.xMax, b.xMax) - left;
            float top = math.max(a.yMin, b.yMin);
            float height = math.min(a.yMax, b.yMax) - top;

            return new AxisAlignedBounds2D(left, top, left + width, top + height);
        }

        public bool OverlapOrContain(in AxisAlignedBounds2D b) {
            return xMax >= b.xMin && xMin <= b.xMax && yMax >= b.yMin && yMin <= b.yMax;
        }

    }

    public struct AxisAlignedBounds2DUShort {

        public ushort xMin;
        public ushort yMin;
        public ushort xMax;
        public ushort yMax;

        public AxisAlignedBounds2DUShort(ushort xMin, ushort yMin, ushort xMax, ushort yMax) {
            this.xMin = xMin;
            this.yMin = yMin;
            this.xMax = xMax;
            this.yMax = yMax;
        }

        public ushort Width {
            get => (ushort) (xMax - xMin);
        }

        public ushort Height {
            get => (ushort) (yMax - yMin);
        }

        public static unsafe bool HasValue(in AxisAlignedBounds2DUShort aabb) {
            fixed (void* longVal = &aabb) {
                return *(long*)longVal != 0;
            }
        }

    }

}