using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class SteamCoreDefenseLevelBuilder
{
    const string Path = "Assets/Scenes/SteamCoreDefenseLevel.unity";
    static Dictionary<string, Material> mats = new Dictionary<string, Material>();

    [MenuItem("Steam Core Defense/Open Level")]
    public static void OpenLevel()
    {
        if (System.IO.File.Exists(Path))
        {
            EditorSceneManager.OpenScene(Path);
            Debug.Log("Opened existing level. Your manual changes were preserved.");
            return;
        }

        BuildLevelInternal();
    }

    [MenuItem("Steam Core Defense/Rebuild Level (Overwrite)")]
    public static void RebuildLevel()
    {
        if (EditorUtility.DisplayDialog("Rebuild level", "This will overwrite the current level and delete manual changes. Continue?", "Overwrite", "Cancel"))
        {
            BuildLevelInternal();
        }
    }

    private static void BuildLevelInternal()
    {
        mats.Clear();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(.12f,.15f,.2f); RenderSettings.fog = true; RenderSettings.fogColor = new Color(.08f,.1f,.13f); RenderSettings.fogDensity = .012f;
        var env = new GameObject("Environment"); var gameplay = new GameObject("Gameplay"); var pipes = new GameObject("Pipes"); var spawns = new GameObject("SpiderSpawnPoints");
        Cube("Floor", env.transform, new Vector3(0,-.25f,0), new Vector3(24,.5f,20), M("Floor",new Color(.16f,.18f,.2f)));
        Cube("BackWall",env.transform,new Vector3(0,3,10),new Vector3(24,6,.5f),M("Wall",new Color(.11f,.13f,.16f)));
        Cube("LeftWall",env.transform,new Vector3(-12,3,0),new Vector3(.5f,6,20),M("Wall",new Color(.11f,.13f,.16f)));
        Cube("RightWall",env.transform,new Vector3(12,3,0),new Vector3(.5f,6,20),M("Wall",new Color(.11f,.13f,.16f)));
        Cube("FrontWall",env.transform,new Vector3(0,3,-10),new Vector3(24,6,.5f),M("Wall",new Color(.11f,.13f,.16f)));
        Cube("Roof",env.transform,new Vector3(0,6.2f,0),new Vector3(24,.4f,20),M("Roof",new Color(.06f,.07f,.09f)));
        var boiler=Cylinder("Boiler",gameplay.transform,new Vector3(0,2.2f,1.2f),new Vector3(3.2f,4.4f,3.2f),M("Boiler",new Color(.25f,.27f,.3f)));
        Cylinder("BoilerTop",boiler.transform,new Vector3(0,2.5f,0),new Vector3(1.8f,.45f,1.8f),M("HotMetal",new Color(.55f,.12f,.04f)));
        Sphere("BoilerCore",boiler.transform,new Vector3(0,.35f,0),Vector3.one*.85f,M("CoreGlow",new Color(1f,.25f,.03f),true));
        Label("OVERHEATING BOILER",boiler.transform,new Vector3(0,2.7f,-1.65f),.32f,Color.red);
        var cooler=Cube("Cooler",gameplay.transform,new Vector3(0,1.1f,-4.5f),new Vector3(3.5f,2.2f,2.2f),M("Cooler",new Color(.05f,.3f,.55f)));
        Label("COOLER / WATER",cooler.transform,new Vector3(0,1.4f,-1.15f),.3f,Color.cyan); Cylinder("WaterTank",cooler.transform,new Vector3(0,1.7f,0),new Vector3(1.3f,1.3f,1.3f),M("Water",new Color(.03f,.45f,.8f)));
        Vector3[] valves={new Vector3(-8,1.3f,6.5f),new Vector3(8,1.3f,6.5f),new Vector3(-8,1.3f,-5.5f)};
        for(int i=0;i<valves.Length;i++){var v=Cylinder("Valve_0"+(i+1),gameplay.transform,valves[i],new Vector3(1.2f,2.6f,1.2f),M("Valve",new Color(.8f,.45f,.05f))); Cylinder("ValveHandle",v.transform,new Vector3(0,0f,0),new Vector3(.14f,.9f,.14f),M("Handle",new Color(.95f,.7f,.1f))).transform.localRotation=Quaternion.Euler(90,0,0); CreateValveSign(v.transform,new Vector3(0,-.35f,0),i+1); CreateSteam(v.transform,new Vector3(0,2.6f,0),gameplay.transform);}
        foreach(var v in valves) Pipe(pipes.transform,new Vector3(0,3.5f,1.2f),v+Vector3.up*1.7f);
        Pipe(pipes.transform,new Vector3(0,1.8f,1.2f),new Vector3(0,1.8f,-4.5f));
        foreach(var v in valves) CreateSteam(v + Vector3.up * 1.9f, gameplay.transform);
        CreateSteam(new Vector3(0,3.8f,1.2f), gameplay.transform);
        Vector3[] points={new Vector3(-10,.3f,8),new Vector3(10,.3f,8),new Vector3(-10,.3f,-8),new Vector3(10,.3f,-8)};
        for(int i=0;i<points.Length;i++){var p=new GameObject("SpiderSpawn_"+(i+1));p.transform.SetParent(spawns.transform);p.transform.position=points[i];Cylinder("SpawnMarker",p.transform,Vector3.zero,new Vector3(.6f,.05f,.6f),M("Spawn",new Color(.65f,.05f,.05f),true));}
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1.1f, -8);
        var controller = player.AddComponent<CharacterController>(); controller.height = 2f; controller.radius = .45f; controller.center = Vector3.zero;
        var cameraPivot = new GameObject("CameraPivot"); cameraPivot.transform.SetParent(player.transform); cameraPivot.transform.localPosition = new Vector3(0, .65f, 0);
        var cam = cameraPivot.AddComponent<Camera>(); cam.tag = "MainCamera"; cam.fieldOfView = 75f;
        cameraPivot.AddComponent<AudioListener>();
        var wrenchType = System.Type.GetType("WrenchTool, Assembly-CSharp"); if (wrenchType != null) cameraPivot.AddComponent(wrenchType);
        var playerController = player.AddComponent<PlayerController>();
        var field = typeof(PlayerController).GetField("cameraPivot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.SetValue(playerController, cameraPivot.transform);
        Label("PLAYER",player.transform,new Vector3(0,1.5f,0),.3f,Color.green);
        var gameplayType = System.Type.GetType("CoreGameplay, Assembly-CSharp"); if (gameplayType != null) boiler.AddComponent(gameplayType);
        MakeReactorDetails(gameplay.transform);
        var sun=new GameObject("Laboratory Light");var l=sun.AddComponent<Light>();l.type=LightType.Directional;l.intensity=.45f;l.color=new Color(.55f,.7f,1f);sun.transform.rotation=Quaternion.Euler(50,-30,0); Light("BoilerWarningLight",new Vector3(0,5.5f,1.2f),Color.red,0,8); Light("CoolerLight",new Vector3(0,3,-4.5f),Color.cyan,1.5f,6); Light("CeilingLight_1",new Vector3(-6,5.5f,4),new Color(.5f,.7f,1f),2.5f,10); Light("CeilingLight_2",new Vector3(6,5.5f,4),new Color(.5f,.7f,1f),2.5f,10);
        EditorSceneManager.SaveScene(scene,Path); Debug.Log("Created "+Path);
    }

    static GameObject Cube(string n,Transform p,Vector3 pos,Vector3 scale,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;return g;}
    static GameObject Cylinder(string n,Transform p,Vector3 pos,Vector3 scale,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(p);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;return g;}
    static GameObject Sphere(string n,Transform p,Vector3 pos,Vector3 scale,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;return g;}
    static void Pipe(Transform p,Vector3 a,Vector3 b){var d=b-a;var g=Cylinder("Pipe",p,(a+b)/2,new Vector3(.35f,d.magnitude/2,.35f),M("Pipe",new Color(.32f,.34f,.37f)));g.transform.up=d.normalized;}
    static void Label(string t,Transform p,Vector3 pos,float size,Color c){}

    static void Light(string n,Vector3 pos,Color c,float intensity,float range){var g=new GameObject(n);g.transform.position=pos;var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=c;l.intensity=intensity;l.range=range;}
    static void MakeReactorDetails(Transform parent)
    {
        for(int i=0;i<8;i++){float a=i*Mathf.PI*2f/8f;Vector3 p=new Vector3(Mathf.Cos(a)*2.1f,.7f,1.2f+Mathf.Sin(a)*2.1f);Cylinder("ReactorSupport",parent,p,new Vector3(.18f,1.4f,.18f),M("Support",new Color(.1f,.12f,.15f)));}
        for(int i=0;i<6;i++){float a=i*Mathf.PI*2f/6f;Vector3 p=new Vector3(Mathf.Cos(a)*5.2f,.15f,Mathf.Sin(a)*5.2f);Cylinder("FloorLight",parent,p,new Vector3(.18f,.03f,.18f),M("WarningLight",Color.red,true));}
        Cylinder("ReactorBase",parent,new Vector3(0,.15f,1.2f),new Vector3(4.8f,.3f,4.8f),M("Base",new Color(.08f,.1f,.12f)));
    }

    static Material M(string n,Color c,bool emission=false){if(mats.TryGetValue(n,out var old))return old;var s=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");var m=new Material(s){name=n};m.color=c;if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*2);}mats[n]=m;return m;}
    static void CreateValveSign(Transform parent,Vector3 pos,int number){var g=new GameObject("ValveSign_"+number);g.transform.SetParent(parent);g.transform.localPosition=pos;g.transform.localRotation=Quaternion.Euler(0,180,0);var tm=g.AddComponent<TextMesh>();tm.text="VALVE "+number;tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontSize=42;tm.characterSize=.18f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=Color.yellow;}
    static void CreateSteam(Vector3 pos,Transform root){CreateSteam(root,pos,root);}
    static void CreateSteam(Transform parent,Vector3 pos,Transform root){var g=new GameObject("SteamEffect");g.transform.SetParent(root);g.transform.position=parent.position+pos;var p=g.AddComponent<ParticleSystem>();var main=p.main;main.loop=true;main.playOnAwake=false;main.startLifetime=1.2f;main.startSpeed=1.5f;main.startSize=.35f;main.startColor=new Color(.8f,.9f,1f,.65f);var emission=p.emission;emission.rateOverTime=35f;var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=18f;shape.radius=.12f;var r=p.GetComponent<ParticleSystemRenderer>();r.material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")??Shader.Find("Particles/Standard Unlit"));r.material.color=new Color(.8f,.9f,1f,.65f);}
}
