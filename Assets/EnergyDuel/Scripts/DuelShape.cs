using System.Collections.Generic;
using UnityEngine;

namespace EnergyDuel
{
    // Cada topología se comparte entre los dos jugadores. El tamaño y color
    // pertenecen al Transform y al material del prefab, respectivamente.
    [ExecuteAlways, RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class DuelShape : MonoBehaviour
    {
        public enum Forma { Hoja, Prisma, Disco, AroHexagonal, Diana, Marca }
        public Forma forma;
        private static readonly Mesh[] cache = new Mesh[6];

        private void OnEnable()
        {
            int i=(int)forma;
            if (!cache[i]) cache[i]=Crear(forma);
            GetComponent<MeshFilter>().sharedMesh=cache[i];
        }

        private static Mesh Crear(Forma f)
        {
            var vertices=new List<Vector3>();
            var indices=new List<int>();
            void Cara(Vector3 a,Vector3 b,Vector3 c)
            {
                int n=vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                indices.Add(n); indices.Add(n+1); indices.Add(n+2);
            }
            void Cuadro(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                Cara(a,b,c); Cara(a,c,d);
            }
            if (f==Forma.Hoja)
            {
                // Hoja con sección de diamante, hombros biselados y punta.
                Vector3[] seccion={Vector3.left,Vector3.forward*.52f,Vector3.right,Vector3.back*.52f};
                for (int i=0;i<4;i++)
                {
                    var a=seccion[i]*.5f; var b=seccion[(i+1)%4]*.5f;
                    var c=b*.78f+Vector3.up*.84f; var d=a*.78f+Vector3.up*.84f;
                    Cuadro(a,b,c,d); Cara(d,c,Vector3.up);
                }
            }
            else
            {
                int lados=f==Forma.Diana?48:f==Forma.Marca?4:6;
                float interior=f==Forma.Diana?.952f:f==Forma.AroHexagonal?.945f:0;
                for (int i=0;i<lados;i++)
                {
                    float a=(float)i/lados*Mathf.PI*2+Mathf.PI/6;
                    float b=(float)(i+1)/lados*Mathf.PI*2+Mathf.PI/6;
                    var u=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                    var v=new Vector3(Mathf.Cos(b),Mathf.Sin(b),0);
                    var z=Vector3.forward*.5f;
                    Cuadro(u-z,v-z,v+z,u+z);
                    if (interior>0)
                    {
                        Cuadro(u+z,v+z,v*interior+z,u*interior+z);
                        Cuadro(u-z,u*interior-z,v*interior-z,v-z);
                    }
                    else { Cara(Vector3.zero+z,u+z,v+z); Cara(Vector3.zero-z,v-z,u-z); }
                }
            }
            var mesh=new Mesh {name="Energy Duel · "+f,hideFlags=HideFlags.HideAndDontSave};
            mesh.SetVertices(vertices); mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
