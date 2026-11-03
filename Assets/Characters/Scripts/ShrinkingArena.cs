using UnityEngine;

namespace GuerraDeEmpujes
{
    public sealed class ShrinkingArena : MonoBehaviour
    {
        public const int MaxPlayers = 8;
        public const float FullRadius = 15;
        public const float WideX = 22, WideZ = 13.5f;
        public MobileRoundGame game;
        public float Radius { get; private set; } = FullRadius;
        public bool Wide { get; private set; }
        Transform platform;
        Camera arenaCamera;
        float lastAspect;
        Transform followedPlayer;
        Vector3 cameraFocus;
        Vector3 followVelocity;

        public static float StartingRadius(int round) => Mathf.Max(8.75f, FullRadius - .75f * Mathf.Max(0, round - 1));
        public static float RadiusAt(int round, float secondsLeft)
        {
            float start = StartingRadius(round);
            return Mathf.Lerp(start, Mathf.Max(5.6f, start * .45f), Mathf.Clamp01((75 - secondsLeft) / 75));
        }

        void Awake()
        {
            game = GetComponent<MobileRoundGame>();
            arenaCamera = Camera.main;
            // The old decorative magma mesh had upright rocks above the enlarged playing surface.
            // Render the animated material directly on the physical lava bed.
            var lava=FindFirstObjectByType<LavaSurface>();
            var oldMagma=GameObject.Find("Lava_Emissive");
            if(lava && oldMagma){
                var filter=lava.gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh=lava.GetComponent<MeshCollider>().sharedMesh;
                var renderer=lava.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=oldMagma.GetComponent<Renderer>().sharedMaterial;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                oldMagma.SetActive(false);
                var crust=GameObject.Find("Lava_Painted");
                if(crust)crust.SetActive(false);
            }
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name == "Platform_Painted" || t.name == "Surface_Painted" || t.name.StartsWith("Walkable surface"))
                    t.gameObject.SetActive(false);
        }

        public void Configure(int participants, int round)
        {
            bool wide = participants > 4;
            if (!platform || wide != Wide)
            {
                if (platform) { platform.gameObject.SetActive(false); Destroy(platform.gameObject); }
                Wide = wide;
                platform = Instantiate(Resources.Load<GameObject>("Battlefield/" + (wide ? "GrandCourt" : "RoundCourt")), transform).transform;
                FrameCamera();
            }
            SetRadius(StartingRadius(round));
        }

        void FrameCamera()
        {
            if (!arenaCamera) return;
            lastAspect = arenaCamera.aspect;
            arenaCamera.orthographic = true;
            arenaCamera.orthographicSize = 8.8f;
            arenaCamera.transform.rotation = Quaternion.Euler(45, 35, 0);
        }

        void LateUpdate()
        {
            if (!arenaCamera || !game || !game.Player) return;
            var player = game.Player.transform;
            var target = new Vector3(player.position.x, 0, player.position.z);
            // Snap on respawn or teleport. Ordinary movement uses a short, damped follow.
            if (followedPlayer != player || Vector3.Distance(cameraFocus, target) > 5)
            {
                followedPlayer = player;
                cameraFocus = target;
                followVelocity = Vector3.zero;
            }
            else
            {
                cameraFocus = Vector3.SmoothDamp(cameraFocus, target, ref followVelocity, .12f,
                    Mathf.Infinity, Time.unscaledDeltaTime);
            }
            arenaCamera.transform.position = cameraFocus - arenaCamera.transform.forward * 30;
        }

        public Vector3 SpawnPoint(int index)
        {
            Vector2 gate = SpawnGates[index];
            float scale = Radius / FullRadius;
            return new Vector3(gate.x * (Wide ? WideX : FullRadius) * scale, .04f, gate.y * (Wide ? WideZ : FullRadius) * scale);
        }
        public static readonly Vector2[] SpawnGates = {
            new Vector2(0,-.7f), new Vector2(-.7f,0), new Vector2(0,.7f), new Vector2(.7f,0),
            new Vector2(-.65f,-.64f), new Vector2(.65f,.64f), new Vector2(-.65f,.64f), new Vector2(.65f,-.64f)
        };

        public bool Inside(Vector3 p, float margin)
        {
            if (!Wide) return new Vector2(p.x,p.z).magnitude <= Radius - margin;
            float scale = Radius / FullRadius;
            float dx = WideX * scale - Mathf.Abs(p.x), dz = WideZ * scale - Mathf.Abs(p.z);
            return dx >= margin && dz >= margin && dx + dz >= 2 * scale + margin * 1.4142f;
        }
        public void SetRadius(float value)
        {
            Radius = Mathf.Clamp(value, 5.6f, FullRadius);
            if (platform) platform.localScale = new Vector3(Radius / FullRadius, 1, Radius / FullRadius);
        }
        void Update()
        {
            if (arenaCamera && Mathf.Abs(arenaCamera.aspect-lastAspect)>.01f) FrameCamera();
            if (!game || game.Online && !game.Network.IsHost) return;
            if (game.Phase == ArenaPhase.Fighting && game.BattleMode != LocalBattleMode.Training)
                SetRadius(RadiusAt(game.Round,game.TimeLeft));
        }
    }
}
