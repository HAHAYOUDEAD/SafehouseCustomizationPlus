using Il2Cpp;

namespace SCPlus
{
    [RegisterTypeInIl2Cpp]
    internal class SCPlusDecorationDetector : MonoBehaviour
    {
        public SCPlusDecorationDetector(IntPtr intPtr) : base(intPtr) { }

        public CapsuleCollider? cc;
        public Rigidbody? rb;

        public void Awake()
        {
            cc = this.GetOrAddComponent<CapsuleCollider>();
            rb = this.GetOrAddComponent<Rigidbody>();
            cc.isTrigger = true;
            cc.radius = Settings.options.outlineDistance;
            cc.height = 2f;
            rb.isKinematic = true;
        }

        public void OnDestroy()
        {
            if (cc != null)
            {
                Destroy(cc);
                cc = null;
            }
            if (rb != null)
            {
                Destroy(rb);
                rb = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & (int)Utility.LayerMask.PossibleDecoration) == 0) return;

            if (other.gameObject.TryGetComponentInParent(out DecorationItem di))
            {
                var rr = di.GetRenderers();
                if (rr.Count > 0)
                {
                    int id = rr[0].GetInstanceID();
                    if (!MiscPatches.SkipOutline.inProximity.Contains(id)) MiscPatches.SkipOutline.inProximity.Add(id);
                    ApplyPropertyBlockToRenderers(rr, GameManager.GetSafehouseManager().m_OutlinePropertyBlock);
                }

            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & (int)Utility.LayerMask.PossibleDecoration) == 0) return;
            
            if (other.gameObject.TryGetComponentInParent(out DecorationItem di))
            {
                var rr = di.GetRenderers();
                if (rr.Count > 0)
                {
                    int id = rr[0].GetInstanceID();
                    if (MiscPatches.SkipOutline.inProximity.Contains(id)) MiscPatches.SkipOutline.inProximity.Remove(id);
                    ResetPropertyBlockOnRenderers(di.GetRenderers());
                }
            }
        }
    }
}
