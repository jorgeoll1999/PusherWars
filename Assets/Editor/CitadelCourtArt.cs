using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GuerraDeEmpujes;

// Builds the court as a handful of material batches. All inlay is flat and has no collider.
public static class CitadelCourtArt
{
    const string Root="Assets/Characters/Resources/Battlefield/";
    static readonly Vector3 Up=Vector3.up;
    static Vector3 Point(float x,float z,float y=0) => new Vector3(x,y,z);
    static Vector3 Polar(float r,float a,float y=0) => Point(r*Mathf.Cos(a),r*Mathf.Sin(a),y);
    static Vector3[] Loop(int n,Func<int,Vector3> point) => Enumerable.Range(0,n).Select(point).ToArray();
    static Vector3[] Ring(float r,int n,float angle=0) => Loop(n,i=>Polar(r,angle+2*Mathf.PI*i/n));
    static readonly Dictionary<string,Material> palette=new Dictionary<string,Material>();

    class Sculpt
    {
        readonly List<Vector3> vertices=new List<Vector3>(), normals=new List<Vector3>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly Dictionary<string,List<int>> batches=new Dictionary<string,List<int>>();
        public void Face(string mat,Vector3 normal,params Vector3[] points)
        {
            Vector3 n=Vector3.Cross(points[1]-points[0],points[2]-points[0]).normalized;
            if(Vector3.Dot(n,normal)<0){Array.Reverse(points);n=-n;}
            if(!batches.TryGetValue(mat,out var tris)){tris=new List<int>();batches.Add(mat,tris);}
            int start=vertices.Count;
            foreach(var v in points){
                vertices.Add(v);normals.Add(n);
                Vector2 coord=Mathf.Abs(n.y)>.52f?new Vector2(v.x,v.z):Mathf.Abs(n.x)>.62f?new Vector2(v.z,v.y):new Vector2(v.x,v.y);
                if(mat=="Astrolabe") uv.Add(new Vector2(.5f+v.x/30f,.5f+v.z/30f));
                else if(mat=="Observatory") uv.Add(new Vector2(.5f+v.x/(2*ShrinkingArena.WideX),.5f+v.z/(2*ShrinkingArena.WideZ)));
                else uv.Add(coord/3.65f);
            }
            for(int i=1;i<points.Length-1;i++){tris.Add(start);tris.Add(start+i);tris.Add(start+i+1);}
        }
        public void Band(string mat,Vector3[] a,Vector3[] b,bool top=false)
        {
            for(int i=0;i<a.Length;i++){int j=(i+1)%a.Length;
                Face(mat,top?Up:(a[i]+a[j]).normalized,a[i],a[j],b[j],b[i]);}
        }
        public void Stroke(string mat,Vector3 a,Vector3 b,float width)
        {
            var side=Vector3.Cross(Up,(b-a).normalized)*width*.5f;
            Face(mat,Up,a-side,b-side,b+side,a+side);
        }
        public void Outline(string mat,Vector3[] path,float width)
        {for(int i=0;i<path.Length;i++)Stroke(mat,path[i],path[(i+1)%path.Length],width);}
        public void Save(string name,Vector3[] collision=null)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=batches.Count;
            int index=0;foreach(var batch in batches.Values)mesh.SetTriangles(batch,index++);
            mesh.RecalculateBounds();mesh.RecalculateTangents();mesh=SaveMesh(mesh,name);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;
            go.GetComponent<MeshRenderer>().sharedMaterials=batches.Keys.Select(k=>palette[k]).ToArray();
            if(collision!=null){
                var walk=new Mesh{name=name+"Walk"};walk.vertices=collision;
                var indices=new List<int>();for(int i=1;i<collision.Length-1;i++)indices.AddRange(new[]{0,i+1,i});
                walk.triangles=indices.ToArray();go.AddComponent<MeshCollider>().sharedMesh=SaveMesh(walk,walk.name);
            }
            PrefabUtility.SaveAsPrefabAsset(go,Root+name+".prefab");UnityEngine.Object.DestroyImmediate(go);
        }
    }
    static Mesh SaveMesh(Mesh mesh,string name)
    {
        string path=Root+name+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!previous){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        previous.Clear();
        previous.vertices=mesh.vertices;previous.normals=mesh.normals;
        previous.uv=mesh.uv;previous.tangents=mesh.tangents;
        previous.subMeshCount=mesh.subMeshCount;
        for(int i=0;i<mesh.subMeshCount;i++) previous.SetTriangles(mesh.GetTriangles(i),i);
        previous.RecalculateBounds();previous.UploadMeshData(false);
        EditorUtility.SetDirty(previous);UnityEngine.Object.DestroyImmediate(mesh);return previous;
    }
    static void Material(string name,string hex,bool textured,float metallic=0,float smooth=.24f)
    {
        string path=Root+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        ColorUtility.TryParseHtmlString("#"+hex,out var color);m.SetColor("_BaseColor",color);
        m.SetTexture("_BaseMap",textured?AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"CourtSlate.png"):null);
        m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);palette[name]=m;
    }
    [MenuItem("Guerra de empujes/Bake Caldera courts")]
    public static void Bake()
    {
        if(!AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"CourtSlate.png"))throw new FileNotFoundException("CourtSlate.png");
        foreach(var name in new[]{"Astrolabe","Observatory"}){
            var floorTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+name+".png");
            if(!floorTexture)throw new FileNotFoundException(name);
            Material(name,"FFFFFF",false);
            palette[name].SetTexture("_BaseMap",floorTexture);
        }
        Material("Rim","9BAFD4",true);Material("Graphite","4C5666",true);
        Material("AmberBronze","B6884D",false,.35f,.43f);Material("Champagne","E8C077",false,.26f,.5f);
        Material("Basalt","A58E71",true);Material("Lichen","9C9F78",true);Material("Granite","D6C5A8",true);
        Material("Azure","5EADFF",false,.12f);Material("Jade","59D5C6",false,.12f);
        Material("Peridot","A2DA70",false,.12f);Material("Amethyst","CF83C8",false,.12f);
        Court(false);Court(true);Obelisk();Boulder();AssetDatabase.SaveAssets();
    }
    static Vector3[] Rectangle()
    {
        float x=ShrinkingArena.WideX,z=ShrinkingArena.WideZ,c=2;
        return new[]{Point(x,z-c),Point(x-c,z),Point(-x+c,z),Point(-x,z-c),Point(-x,-z+c),Point(-x+c,-z),Point(x-c,-z),Point(x,-z+c)};
    }
    static Vector3[] Offset(Vector3[] path,float scale,float y,Vector3 offset=default)
        => path.Select(p=>p*scale+Up*y+offset).ToArray();
    static void Court(bool wide)
    {
        var art=new Sculpt();
        var edge=wide?Rectangle():Ring(ShrinkingArena.FullRadius,96);
        art.Face(wide?"Observatory":"Astrolabe",Up,edge.ToArray());
        art.Band("Graphite",Offset(edge,1,-.65f),Offset(edge,1,0));
        art.Band("Rim",Offset(edge,.983f,.005f),Offset(edge,1,.005f),true);
        art.Outline("AmberBronze",Offset(edge,.982f,.015f),.065f);
        var gems=new[]{"Azure","Jade","Peridot","Amethyst"};
        for(int i=0;i<(wide?8:4);i++){
            var g=ShrinkingArena.SpawnGates[i];
            var pos=Point(g.x*(wide?ShrinkingArena.WideX:ShrinkingArena.FullRadius),g.y*(wide?ShrinkingArena.WideZ:ShrinkingArena.FullRadius));
            float radius=i<4?1.39f:1.02f;var pad=Ring(radius,8,Mathf.PI/8);
            art.Face("Graphite",Up,Offset(pad,1,.02f,pos));art.Face("Rim",Up,Offset(pad,.88f,.023f,pos));
            art.Outline("AmberBronze",Offset(pad,.99f,.027f,pos),.075f);
            art.Outline(gems[i%4],Offset(pad,.8f,.031f,pos),.10f);
            art.Outline(gems[i%4],Offset(Ring(radius*.41f,4),1,.034f,pos),.08f);
            var outDir=pos.normalized;var tangent=Vector3.Cross(Up,outDir);
            for(int k=-1;k<=1;k++){var notch=pos+outDir*(radius+.18f)+tangent*k*.23f+Up*.018f;
                art.Stroke(gems[i%4],notch,notch+outDir*(k==0?.38f:.24f),.10f);}
        }
        art.Save(wide?"GrandCourt":"RoundCourt",edge);
    }
    static void Obelisk()
    {
        var s=new Sculpt();var oct=Ring(1,8,Mathf.PI/8);
        float[] r={.97f,.97f,.82f,.77f,.77f,.64f,.60f,.57f,.48f,.46f,.52f,.48f,.07f};
        float[] y={0,.16f,.23f,.29f,.44f,.52f,.57f,.61f,1.83f,1.88f,1.93f,2.01f,2.53f};
        string[] skins={"Graphite","Granite","AmberBronze","Basalt","Granite","Graphite","Champagne","Rim","AmberBronze","Granite","AmberBronze","Granite"};
        for(int i=0;i<skins.Length;i++)s.Band(skins[i],Offset(oct,r[i],y[i]),Offset(oct,r[i+1],y[i+1]));
        s.Face("Champagne",Up,Offset(oct,r[r.Length-1],y[y.Length-1]));
        for(int i=0;i<4;i++){
            var n=Polar(1,i*Mathf.PI/2);var side=Vector3.Cross(Up,n);
            var low=n*.516f+Up*.84f;var mid=n*.50f+Up*1.34f;var high=n*.468f+Up*1.72f;
            s.Face("AmberBronze",n,low,mid+side*.17f,high,mid-side*.17f);
            var center=mid+n*.016f;s.Face("Jade",n,center-Up*.21f,center+side*.078f,center+Up*.19f);
            s.Face("Rim",n,center-Up*.21f,center+Up*.19f,center-side*.078f);
        }
        s.Save("SealObelisk");
    }
    static void Boulder()
    {
        var s=new Sculpt();var rnd=new System.Random(83);
        void Stone(Vector3 at,float x,float z,float h)
        {
            var baseRing=Loop(9,i=>{float a=(float)(i+rnd.NextDouble()*.30)*Mathf.PI*2/9;
                return Point(x*Mathf.Cos(a),z*Mathf.Sin(a));});
            var bottom=baseRing.Select(p=>at+p*.86f).ToArray();
            var mid=baseRing.Select(p=>at+p*(.92f+.13f*(float)rnd.NextDouble())+Up*h*(.32f+.22f*(float)rnd.NextDouble())).ToArray();
            var top=baseRing.Select(p=>at+p*(.46f+.25f*(float)rnd.NextDouble())+Point(-.09f,.07f,h*(.78f+.2f*(float)rnd.NextDouble()))).ToArray();
            for(int i=0;i<9;i++){int j=(i+1)%9;var side=baseRing[i];s.Face("Basalt",side,bottom[i],bottom[j],mid[i]);s.Face("Graphite",side,bottom[j],mid[j],mid[i]);s.Face("Granite",side,mid[i],mid[j],top[j]);s.Face("Basalt",side,mid[i],top[j],top[i]);s.Face(i%4==0?"Lichen":"Granite",Up,top[i],top[j],at+Up*h);}
        }
        Stone(Point(-.10f,.16f),.85f,.70f,1.74f);Stone(Point(.43f,-.34f),.48f,.43f,1.02f);
        Stone(Point(-.52f,-.38f),.40f,.34f,.58f);s.Save("WeatheredRock");
    }
}
