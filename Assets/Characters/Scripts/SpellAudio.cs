using UnityEngine;
namespace GuerraDeEmpujes
{
 public static class SpellAudio
 {
  static readonly System.Collections.Generic.Dictionary<string,AudioClip> clips=new System.Collections.Generic.Dictionary<string,AudioClip>();
  public static AudioSource Play(string name,Vector3 position,float volume=.55f)
  {
   AudioClip clip;if(!clips.TryGetValue(name,out clip)){clip=Resources.Load<AudioClip>("SpellAudio/"+name);if(clip)clips[name]=clip;}
   if(!clip)return null;if(name!="BubblePop")OnlineSession.Emit("audio:"+name,position,volume:volume);var go=new GameObject("Spell sound "+name);go.transform.position=position;var source=go.AddComponent<AudioSource>();source.clip=clip;source.volume=volume;source.pitch=1;source.spatialBlend=.35f;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=3;source.maxDistance=45;source.dopplerLevel=0;source.Play();Object.Destroy(go,clip.length+.1f);return source;
  }
 }
}
