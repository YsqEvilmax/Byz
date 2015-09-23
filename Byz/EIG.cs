using System;
using System.Collections.Generic;
using System.Linq;

namespace Byz
{
    public class Label<T>
        : List<T>
    {
        public Label() : base() { }
        public Label(IEnumerable<T> collection) : base(collection){ }

        public override bool Equals(object obj)
        {
            return this.ToString().Equals((obj as Label<T>).ToString());
        }
        public override string ToString()
        {
            Trim();
            String result = null;
            foreach(T t in this)
            {
                result += "," + t.ToString();
            }
            return result != null ? result.Remove(0, 1) : result;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void Trim()
        {
            this.RemoveAll(x => x.Equals(""));
        }

        public static bool operator ==(Label<T> label1, Label<T> label2)
        {  
            return Object.Equals(label1, label2);
        }
        public static bool operator !=(Label<T> label1, Label<T> label2)
        {
            return !Object.Equals(label1, label2);
        }

        public static Label<T> operator +(Label<T> label1, Label<T> label2)
        {
            Label<T> targetLabel = new Label<T>();
            targetLabel.AddRange(label1);
            targetLabel.AddRange(label2);
            targetLabel.Trim();
            return targetLabel;
        }

        public static Label<T> operator +(Label<T> label, T t)
        {
            Label<T> targetLabel = new Label<T>();
            if (label != null) { targetLabel.AddRange(label); }
            targetLabel.Add(t);
            targetLabel.Trim();
            return targetLabel;
        }

        public static Label<T> operator -(Label<T> label, T t)
        {
            Label<T> targetLabel = new Label<T>();
            if (label != null) targetLabel.AddRange(label);
            targetLabel.Remove(t);
            targetLabel.Trim();
            return targetLabel;
        }
    }

    public class EIGNode<T>
        : ICloneable
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

            this.label = new Label<string>();
            foreach(String s in label.Split(','))
            {
                this.label.Add(s);
            }
        }

        public EIGNode(EIGNode<T> parent, String label, int level)
            : this(parent, label)
        {
            this.level = level;
        }

        public object Clone()
        {
            var Obj = new EIGNode<T>();
            //Obj.parent = this.parent != null ? this.parent.Clone() as EIGNode<T> : null;
            Obj.children = this.children.Clone<EIGNode<T>>();
            Obj.label = new Label<String>(this.label.Clone<String>());
            Obj.value = this.value;
            return Obj;
        }

        public void Build(Label<String> labelSet, int depth)
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

        public EIGNode<T> Find(Label<String> labelTarget)
        {
            EIGNode<T> node = null;
            if (labelTarget.Equals(this.label))
            {
                return this;
            }
            foreach (EIGNode<T> n in children)
            {
                node = n.Find(labelTarget);
                if (node != null) return node;
            }
            return node;
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
                node.Add(this.Clone() as EIGNode<T>);
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
            children.Add(new EIGNode<T>(this, (label + labelExt).ToString(), this.level + 1));
        }

        public void AddChildren(Label<String> labelSet)
        {
            RestLabel(ref labelSet);
            foreach (string s in labelSet)
            {
                AddChild(s);
            }
        }

        public void RestLabel(ref Label<String> labelSet)
        {          
            foreach (String s in label)
            {
                labelSet = labelSet - s;
            }
        }

        //public T Majority()
        //{
        //    Dictionary<T, int> collection = new Dictionary<T, int>();
        //    try
        //    {               
        //        foreach (EIGNode<T> n in children)
        //        {
        //            T major = n.Majority();
        //            if (collection.Keys.Contains<T>(major))
        //            {
        //                collection[n.value]++;
        //            }
        //            else
        //            {
        //                collection.Add(major, 1);
        //            }
        //        }
        //        if (collection.Count() == 0) { collection.Add(this.value, 1); }
        //        else
        //        {
        //            collection = collection.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        //        }
        //        this.value = collection.First().Key;
        //    }
        //    catch(Exception e)
        //    {
        //        Console.WriteLine(e);
        //    }
        //    return this.value;
        //}

        public void Majority()
        {
            try
            {
                Dictionary<T, int> collection = new Dictionary<T, int>();
                foreach(EIGNode<T> n in children)
                {
                    n.Majority();
                    if(collection.Keys.Contains(n.value))
                    {
                        collection[n.value]++;
                    }
                    else
                    {
                        collection.Add(n.value, 1);
                    }
                }
                if(collection.Count > 0)
                {
                    this.value = collection.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).First().Key;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override String ToString()
        {
            return "(" + label.ToString() + ":" + value.ToString() + ")";
        }

        private EIGNode<T> parent;
        private List<EIGNode<T>> children;
        public int level { get; private set; }
        public Label<String> label { get;  set; }
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
            this.labelSet = new Label<string>(labelSet.Split(',').OrderBy(x => x));
        }

        public void Build(int depth)
        {
            this.depth = depth <= labelSet.Count + 1 ? depth : labelSet.Count + 1;
            root.Build(labelSet, this.depth - 1);
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

        public override String ToString()
        {
            String result = null;
            for(int i = 0; i < depth; i++)
            {
                foreach(String level in root.FindOnLevel(i).Select(x => x.ToString()).ToList())
                {
                    result += level;
                }
                result += "\r\n";
            }
            return result;
        }

        public int depth { get; private set; }
        public Label<String> labelSet;
        public EIGNode<T> root { get; private set; }
    }
}
