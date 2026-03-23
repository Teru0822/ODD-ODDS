using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QubicNS
{
    public class Variant : MonoBehaviour
    {
        public bool Exclusive = true;

        public static void Build(GameObject holder, Rnd rnd, bool deactivateOnly = false)
        {
            buffer.Clear();
            holder.GetComponentsInChildren<Variant>(deactivateOnly, buffer);
            if (buffer.Count == 0)
                return;
            Build(buffer, rnd, deactivateOnly);
        }

        static List<Variant> buffer = new List<Variant>();
        static Dictionary<Transform, List<Variant>> variantsByParent = new Dictionary<Transform, List<Variant>>();

        public static void Build(IEnumerable<Variant> variants, Rnd rnd, bool deactivateOnly = false)
        {
            variantsByParent.Clear();
            foreach (var v in variants)
            {
                if (!variantsByParent.TryGetValue(v.transform.parent, out var list))
                    variantsByParent[v.transform.parent] = list = new List<Variant>();
                list.Add(v);
            }

            foreach (var pair in variantsByParent)
            {
                var parent = pair.Key;
                if (!parent) continue;
                buffer.Clear();
                buffer.AddRange(pair.Value.Where(v => v != null && v.Exclusive));
                if (buffer.Count == 0) continue;
                var selected = rnd.Int(buffer.Count);
                for (int i = 0; i < buffer.Count; i++)
                {
                    var go = buffer[i].gameObject;

                    // destroy unselected object
                    if (i != selected)
                    {
                        go.SetActive(false);
                        if (!deactivateOnly)
                            Helper.DestroySafe(go);
                        continue;
                    }else
                    {
                        if (deactivateOnly)
                            go.SetActive(true);
                    }
                    // destroy variant script
                    if (deactivateOnly)
                    {

                    }
                    else
                    {
                        buffer[i].enabled = false;
                        Helper.DestroySafe(buffer[i]);
                    }
                }
            }
        }
    }
}
