using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Byz
{
    public class EIGNode<T>
    {
        public EIGNode()
        {
            children = new List<EIGNode<T>>();
        }

        public EIGNode(EIGNode<T> parent)
            : this()
        {
            this.parent = parent;
        }

        public EIGNode(EIGNode<T> parent, String label)
            : this(parent)
        {
            this.label = label;
        }

        public EIGNode(EIGNode<T> parent, String label, int level)
            : this(parent, label)
        {
            this.level = level;
        }

        public void Build(String labelSet, int depth)
        {
            if (level < depth)
            {
                AddChildren(labelSet);
                foreach (EIGNode<T> n in children)
                {
                    n.Build(labelSet, depth);
                }
            }
        }

        public EIGNode<T> Find(String labelTarget)
        {
            if (labelTarget == this.label) return this;
            String labelExt = labelTarget[0].ToString();
            EIGNode<T> node = FindChild(labelExt);
            if (node == null) return null;
            return node.Find(labelTarget);
        }

        public List<EIGNode<T>> FindOnLevel(int levelTarget)
        {
            List<EIGNode<T>> node = new List<EIGNode<T>>();
            if (levelTarget < this.level)
            {
                node = parent.FindOnLevel(levelTarget);
            }
            else if (levelTarget == this.level)
            {
                node.Add(this);
            }
            else if (levelTarget > this.level)
            {
                foreach (EIGNode<T> n in children)
                {
                    node.AddRange(n.FindOnLevel(levelTarget));
                }
            }
            return node;
        }

        public EIGNode<T> FindChild(String labelExt)
        {
            if (labelExt == null) return this;
            return children.Find((c) => { return c.label == this.label + labelExt; });
        }

        public void AddChild(String labelExt)
        {
            children.Add(new EIGNode<T>(this, label + labelExt, this.level + 1));
        }

        public void AddChildren(String labelSet)
        {
            String rest = RestLabel(labelSet);
            foreach (char c in rest)
            {
                AddChild(c.ToString());
            }
        }

        public String RestLabel(String labelSet)
        {
            foreach (char c in label)
            {
                labelSet = labelSet.Replace(c.ToString(), "");
            }
            return labelSet;
        }

        public T Majority()
        {
            Dictionary<T, int> collection = new Dictionary<T, int>();
            try
            {               
                foreach (EIGNode<T> n in children)
                {
                    T major = n.Majority();
                    if (collection.Keys.Contains<T>(major))
                    {
                        collection[n.value]++;
                    }
                    else
                    {
                        collection.Add(major, 1);
                    }
                }
                if (collection.Count() == 0) { collection.Add(this.value, 1); }
                collection.OrderByDescending(u => u.Value);             
            }
            catch(Exception e)
            {
            }
            return collection.First().Key;
        }

        private EIGNode<T> parent;
        private List<EIGNode<T>> children;
        public int level { get; private set; }
        public String label { get;  set; }
        public T value { get; set; }
    }

    public class EIGTree<T>
    {
        public EIGTree()
        {
            root = new EIGNode<T>(null, "", 0);
        }

        public EIGTree(T v)
            : this()
        {
            root.value = v;
        }

        public EIGTree(T v, String labelSet)
            : this(v)
        {
            setInitValue(v);
        }

        public void Build(int depth)
        {
            this.depth = depth <= labelSet.Length + 1 ? depth : labelSet.Length + 1;
            root.Build(labelSet, this.depth);
        }

        public void setInitValue(T v)
        {
            this.root.value = v;
        }

        public List<EIGNode<T>> FindOnLevel(int levelTarget)
        {
            if (levelTarget >= depth) return null;
            return root.FindOnLevel(levelTarget);
        }

        public int depth { get; private set; }
        public String labelSet { get; set; }
        public EIGNode<T> root { get; private set; }
    }
}
