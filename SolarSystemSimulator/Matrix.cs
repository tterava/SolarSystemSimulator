using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SolarSystemSimulator
{
    class Matrix
    {
        public readonly Vector3D[] columns = new Vector3D[3];
        public static Matrix IdentityMatrix = new Matrix(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1));

        public Matrix(Vector3D a, Vector3D b, Vector3D c)
        {
            columns[0] = a;
            columns[1] = b;
            columns[2] = c;
        }

        public Matrix()
        {
            for (int i = 0; i < 3; i++) columns[0] = new Vector3D(0, 0, 0);
        }

        public static Matrix GetRotationMatrix(Vector3D unitVector, double angle)
        {
            Vector3D x = unitVector;

            Matrix rotationAxisTensor = new Matrix(new Vector3D(x.X * x.X, x.X * x.Y, x.X * x.Z),
                                                   new Vector3D(x.X * x.Y, x.Y * x.Y, x.Y * x.Z),
                                                   new Vector3D(x.X * x.Z, x.Y * x.Z, x.Z * x.Z));

            Matrix rotationAxisCross = new Matrix(new Vector3D(0, x.Z, -x.Y),
                                                  new Vector3D(-x.Z, 0, x.X),
                                                  new Vector3D(x.Y, -x.X, 0));

            return Math.Cos(angle) * Matrix.IdentityMatrix + Math.Sin(angle) * rotationAxisCross + (1.0 - Math.Cos(angle)) * rotationAxisTensor;
        }

        #region Operators
        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            return new Matrix(m1.columns[0] + m2.columns[0], m1.columns[1] + m2.columns[1], m1.columns[2] + m2.columns[2]);
        }

        public static Matrix operator *(double v, Matrix m1)
        {
            return new Matrix(m1.columns[0] * v, m1.columns[1] * v, m1.columns[2] * v);
        }

        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            Matrix result = new Matrix();
            for (int i = 0; i < 3; i++)
            {
                Vector3D a = m2.columns[i];
                result.columns[i].X = m1.columns[0].X * a.X + m1.columns[1].X * a.Y + m1.columns[2].X * a.Z;
                result.columns[i].Y = m1.columns[0].Y * a.X + m1.columns[1].Y * a.Y + m1.columns[2].Y * a.Z;
                result.columns[i].Z = m1.columns[0].Z * a.X + m1.columns[1].Z * a.Y + m1.columns[2].Z * a.Z;
            }
            return result;
        }

        public static Vector3D operator *(Matrix m, Vector3D v)
        {
            Vector3D result = new Vector3D();
            result.X = m.columns[0].X * v.X + m.columns[1].X * v.Y + m.columns[2].X * v.Z;
            result.Y = m.columns[0].Y * v.X + m.columns[1].Y * v.Y + m.columns[2].Y * v.Z;
            result.Z = m.columns[0].Z * v.X + m.columns[1].Z * v.Y + m.columns[2].Z * v.Z;
            return result;
        }
        #endregion
    }
}
