using System;
using UnityEngine;
using UnityEditor;
using GuerraDeEmpujes;

public static class CalderaLavaChecks
{
    [MenuItem("Guerra de empujes/Test Solid Lava")]
    public static void Check()
    {
        var probe=new GameObject("Temporary lava contact test");
        try
        {
            var body=probe.AddComponent<CapsuleCollider>();body.height=2;body.radius=.4f;
            var hp=probe.AddComponent<ArenaHealth>();hp.ResetHealth();
            var contact=probe.AddComponent<LavaContactDamage>();contact.bodyCollider=body;
            probe.transform.position=new Vector3(0,1,0);Physics.SyncTransforms();contact.TickDamage(1);
            Require(hp.CurrentHealth==100 && !contact.IsOnLava,"Safe center must not damage");
            probe.transform.position=new Vector3(13,.92f,0);Physics.SyncTransforms();contact.TickDamage(1);
            Require(Mathf.Abs(hp.CurrentHealth-80)<.01f && contact.IsOnLava,"Lava deals 20 HP per second");
            contact.TickDamage(.5f);Require(Mathf.Abs(hp.CurrentHealth-70)<.01f,"Damage scales with time");
            probe.transform.position=new Vector3(13,3,0);Physics.SyncTransforms();contact.TickDamage(1);
            Require(hp.CurrentHealth==70 && !contact.IsOnLava,"Airborne character must not take ground damage");
            probe.transform.position=new Vector3(8,1,0);Physics.SyncTransforms();contact.TickDamage(1);
            Require(hp.CurrentHealth==70 && !contact.IsOnLava,"Returning to rock stops damage");
            probe.transform.position=new Vector3(13,.92f,0);Physics.SyncTransforms();contact.TickDamage(100);
            Require(hp.CurrentHealth==0,"Health clamps at zero");
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8;Require(Physics.Raycast(new Vector3(14*Mathf.Cos(a),4,14*Mathf.Sin(a)),Vector3.down,out var hit,10,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponent<LavaSurface>()&&Mathf.Abs(hit.point.y+.08f)<.01f,"Continuous solid lava ring");}
            Debug.Log("CALDERA LAVA PASS: solid floor in 16 directions; safe rock, contact damage, delta time, airborne, return and zero HP verified. Temporary probe removed.");
        }
        finally {UnityEngine.Object.DestroyImmediate(probe);}
    }
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
}
