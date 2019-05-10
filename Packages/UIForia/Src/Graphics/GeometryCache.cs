using System;
using UIForia.Util;
using UnityEngine;

namespace Vertigo {

    public enum GeometryType {

        Physical = 1 << 0,
        SignedDistance = 1 << 1,
        Stroke = 1 << 2,
        PhysicalStroke = Physical | Stroke

    }

    [Flags]
    public enum VertexChannel {

        TextureCoord0 = 1 << 0,
        TextureCoord1 = 1 << 1,
        TextureCoord2 = 1 << 2,
        TextureCoord3 = 1 << 3,
        Color = 1 << 4,
        Normal = 1 << 5

    }
    
    public struct GeometryShape {

        public ShapeType shapeType;
        public GeometryType geometryType;
        public VertexChannel vertexChannels;
        public Bounds bounds;
        public int vertexStart;
        public int vertexCount;
        public int triangleStart;
        public int triangleCount;

    }

    public class GeometryCache {

        public StructList<GeometryShape> shapes;

        public StructList<Vector3> positions;
        public StructList<Vector3> normals;
        public StructList<Color> colors;
        public StructList<Vector4> texCoord0;
        public StructList<Vector4> texCoord1;
        public StructList<Vector4> texCoord2;
        public StructList<Vector4> texCoord3;
        public StructList<int> triangles;

        public int shapeCount => shapes.size;
        public int vertexCount => positions.size;
        public int triangleCount => triangles.size;

        public GeometryCache(int capacity = 8) {
            shapes = new StructList<GeometryShape>();
            positions = new StructList<Vector3>(capacity);
            normals = new StructList<Vector3>(capacity);
            colors = new StructList<Color>(capacity);
            texCoord0 = new StructList<Vector4>(capacity);
            texCoord1 = new StructList<Vector4>(capacity);
            texCoord2 = new StructList<Vector4>(capacity);
            texCoord3 = new StructList<Vector4>(capacity);
            triangles = new StructList<int>(capacity * 2);
        }

        public void EnsureAdditionalCapacity(int vertCount, int triCount) {
            positions.EnsureAdditionalCapacity(vertCount);
            normals.EnsureAdditionalCapacity(vertCount);
            colors.EnsureAdditionalCapacity(vertCount);
            texCoord0.EnsureAdditionalCapacity(vertCount);
            texCoord1.EnsureAdditionalCapacity(vertCount);
            texCoord2.EnsureAdditionalCapacity(vertCount);
            texCoord3.EnsureAdditionalCapacity(vertCount);
            triangles.EnsureAdditionalCapacity(triCount);
        }

        public bool SetVertexColors(int shapeIdx, Color color) {
            if (shapeIdx < 0 || shapeIdx > shapes.size) {
                return false;
            }

            GeometryShape shape = shapes.array[shapeIdx];
            shapes.array[shapeIdx].vertexChannels |= VertexChannel.Color;
            int start = shape.vertexStart;
            int end = start + shape.vertexCount;
            Color[] c = this.colors.array;
            for (int i = start; i < end; i++) {
                c[i] = color;
            }

            return true;
        }

        public bool SetNormals(int shapeIdx, Vector3 normal) {
            if (shapeIdx < 0 || shapeIdx > shapes.size) {
                return false;
            }

            GeometryShape shape = shapes.array[shapeIdx];
            shapes.array[shapeIdx].vertexChannels |= VertexChannel.Normal;
            int start = shape.vertexStart;
            int end = start + shape.vertexCount;
            Vector3[] c = this.normals.array;
            for (int i = start; i < end; i++) {
                c[i] = normal;
            }

            return true;
        }

        public bool SetVertexChannels(int shapeIdx, VertexChannel channel) {
            if (shapeIdx < 0 || shapeIdx > shapes.size) {
                return false;
            }
            shapes.array[shapeIdx].vertexChannels = channel;
            return true;
        }

        public void Clear() {
            shapes.QuickClear();
            positions.QuickClear();
            normals.QuickClear();
            colors.QuickClear();
            texCoord0.QuickClear();
            texCoord1.QuickClear();
            texCoord2.QuickClear();
            texCoord3.QuickClear();
            triangles.QuickClear();
        }

        public int GetTextureCoord0(int idx, ref Vector4[] retn) {
            if (idx < 0 || idx > shapes.size) {
                return 0;
            }

            GeometryShape shape = shapes[idx];
            retn = retn ?? new Vector4[shape.vertexCount];
            if (retn.Length < shape.vertexCount) {
                Array.Resize(ref retn, shape.vertexCount);
            }

            Array.Copy(texCoord0.array, shape.vertexStart, retn, 0, shape.vertexCount);
            return shape.vertexCount;
        }

        public int GetTextureCoord0(int idx, StructList<Vector4> retn) {
            if (idx < 0 || idx > shapes.size) {
                return 0;
            }

            GeometryShape shape = shapes[idx];
            retn.EnsureCapacity(shape.vertexCount);
            Array.Copy(texCoord0.array, shape.vertexStart, retn.array, 0, shape.vertexCount);
            retn.size = shape.vertexCount;
            return shape.vertexCount;
        }

        public void GetTextureCoord1(int idx, StructList<Vector4> retn) {
            if (idx < 0 || idx > shapes.size) {
                return;
            }

            GeometryShape shape = shapes[idx];
            retn.EnsureCapacity(shape.vertexCount);
            Array.Copy(texCoord1.array, shape.vertexStart, retn.array, 0, shape.vertexCount);
            retn.size = shape.vertexCount;
        }

        public int GetTextureCoord1(int idx, ref Vector4[] retn) {
            if (idx < 0 || idx > shapes.size) {
                return 0;
            }

            GeometryShape shape = shapes[idx];
            retn = retn ?? new Vector4[shape.vertexCount];
            if (retn.Length < shape.vertexCount) {
                Array.Resize(ref retn, shape.vertexCount);
            }

            Array.Copy(texCoord1.array, shape.vertexStart, retn, 0, shape.vertexCount);
            return shape.vertexCount;
        }

        public void SetTexCoord0(int idx, StructList<Vector4> uvs) {
            if (idx < 0 || idx > shapes.size) {
                return;
            }
            GeometryShape shape = shapes[idx];
            Array.Copy(uvs.array, 0, texCoord0.array, shape.vertexStart, shape.vertexCount);
        }
        
        public void SetTexCoord1(int idx, StructList<Vector4> uvs) {
            if (idx < 0 || idx > shapes.size) {
                return;
            }
            GeometryShape shape = shapes[idx];
            Array.Copy(uvs.array, 0, texCoord1.array, shape.vertexStart, shape.vertexCount);
        }


        public void GetTextureCoord2() {
            throw new NotImplementedException();
        }

        public void GetTextureCoord3() {
            throw new NotImplementedException();
        }

        
    }

}