using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Byz
{
    class Program
    {
        static int NodeCount = 0;
        static int FaultyCount = 0;
        static String allLabelSet = "";

        static void Main()
        {
            //var sw = Stopwatch.StartNew();
            Network.Start().Wait();
            //sw.Stop();
            //sw.ElapsedMilliseconds.Dump("ElapsedMilliseconds");

            //Node.Vertex.Count.Dump("NodeCount");
            //Node.EdgeCount.Dump("EdgeCount");
            //Node.MessageCount.Dump("MessageCount");

            //var SpanningTree =
            //    from k in Node.Vertex.Keys
            //    select new { Node = Node.Vertex[k].Nid, Node.Vertex[k].Parent };
            //SpanningTree.Dump("SpanningTree");

            //EIGTree<int> t = new EIGTree<int>(0);
            //t.setInitValue(1);
            //t.labelSet = "1234";
            //t.Build(3);

            //List<EIGNode<int>> x = null;
            //for (int i=0; i< 3; i++)
            //    x = t.FindOnLevel(i);

            Console.ReadLine();
        }

        protected internal sealed class Network
        {
            static internal async Task Start()
            {
                Node.Reset();
                Console.Write("Please enter the file name to start (such as Generals.txt): ");
                string path;
                if ((path = Console.ReadLine()) != null)
                {
                    path = Directory.GetCurrentDirectory() + "//" + path;
                }
                try
                {
                    if (!File.Exists(path)) throw new Exception("Invalid path!");
                    else
                    {
                        using (StreamReader sr = File.OpenText(path))
                        {
                            String line;
                            int v0 = 0;
                            if ((line = sr.ReadLine()) != null)
                            {
                                String[] indexs = line.Split(' ');
                                NodeCount = int.Parse(indexs[0]);
                                v0 = int.Parse(indexs[1]);
                            }
                            for (int i = 0; i < NodeCount; i++)
                            {
                                if ((line = sr.ReadLine()) != null)
                                {
                                    String node = line.Contains(';') ? line.Substring(0, line.IndexOf(';')) : line;
                                    String[] nodes = node.Split(' ');
                                    int id = int.Parse(nodes[0]);
                                    int v = int.Parse(nodes[1]);
                                    int status = int.Parse(nodes[2]);

                                    if (!allLabelSet.Contains(id.ToString())) { allLabelSet += id.ToString(); }

                                    if (status == 0)
                                    {
                                        new LoyalNode(id, v);
                                    }
                                    else if (status == 1)
                                    {
                                        String script = line.Substring(line.IndexOf(';'));
                                        new TraitorNode(id, v, script);
                                        FaultyCount++;
                                    }
                                }
                            }
                            Node.Vertex = Node.Vertex.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

                            for (int i = 0; i < NodeCount * (NodeCount - 1) / 2; i++)
                            {
                                if ((line = sr.ReadLine()) != null)
                                {
                                    String[] s = line.Split(' ');
                                    Node.Link(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]), int.Parse(s[3]));
                                }       
                            }
                            foreach (int nid in Node.Vertex.Keys)
                            {
                                Node.Link(nid, nid, 0, 0);
                                Node.Vertex[nid].NeighDelay = Node.Vertex[nid].NeighDelay.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                            }

                            var tasks = new List<Task>();
                            foreach (int nid in Node.Vertex.Keys)
                            {
                                (Node.Vertex[nid] as ByzNode).BuildEIG(allLabelSet, FaultyCount + 2);
                                tasks.Add(Node.Vertex[nid].Run()); // no await yet!                                                     //await Node.Vertex[nid].Run();
                            }

                            await Task.WhenAll(tasks);

                            foreach (int nid in Node.Vertex.Keys)
                            {
                                if (Node.Vertex[nid] is TraitorNode)
                                {
                                    Console.WriteLine("{0}:*", nid);
                                }
                                else
                                {
                                    Console.WriteLine("{0}:{1}", nid, (Node.Vertex[nid] as ByzNode).tree.root.Majority());
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        // --- your actual Echo implementation ---
        protected internal class ByzMessage
        {
            protected internal ByzMessage(int round)
            {
                this.round = round;
            }

            protected internal ByzMessage(int round, List<EIGNode<int>> nodes)
                : this(round)
            {
                this.nodes = new List<EIGNode<int>>(nodes);
            }

            public int round { get; private set; }

            public List<EIGNode<int>> nodes { get; private set; }

            public override string ToString()
            {
                String code = null;
                foreach (EIGNode<int> n in nodes)
                {
                    code += n.value.ToString();
                }
                return code;
            }
        }

        protected internal class ByzNode : Node
        {
            static protected internal readonly int MSG_ATTACK = 0;
            static protected internal readonly int MSG_WITHDRAW = 1;
            static protected internal readonly int MSG_FAULT = 2;

            protected internal ByzNode(int nid) : base(nid)
            {
                tree = new EIGTree<int>(nid);
            }

            protected internal ByzNode(int nid, int val) : this(nid)
            {
                tree.setInitValue(val);
            }

            public void BuildEIG(String labelSet, int depth)
            {
                char[] temp = labelSet.ToCharArray();
                Array.Sort(temp);
                tree.labelSet = new String(temp);
                tree.Build(depth);
            }

            public virtual async Task Send(int round) { }

            public EIGTree<int> tree { get; private set; }
        }

        protected internal class TraitorNode : ByzNode
        {
            protected internal TraitorNode(int nid, int val) : base(nid, val)
            {
            }

            protected internal TraitorNode(int nid, int val, String script)
                : this(nid, val)
            {
                this.script = script;
            }


            protected internal override async Task<object> Run()
            {
                Init();
                for (int i = 0; i < FaultyCount + 1; i++)
                {
                    String[] scriptALl = this.script.Split(';');
                    //find all nodes on last level
                    List<EIGNode<int>> lastLevel = tree.root.FindOnLevel(i);
                    //remove the nodes on last level whose label contains process id
                    lastLevel.RemoveAll(c => c.label.Contains(this.Nid.ToString()));

                    String[] msg = scriptALl[i + 1].Split(' ');
                    foreach (int n in NeighDelay.Keys)
                    {
                        //fake
                        int copy = 0;
                        foreach (char c in msg[n - 1])
                        {
                            lastLevel[copy].label = n.ToString();
                            lastLevel[copy++].value = int.Parse(c.ToString());
                        }
                        ByzMessage x = new ByzMessage(i, lastLevel);
                        //await 
                        PostAsync(x, n, NeighDelay[n]);
                    }
                    //if (TRACE_POST) Console.WriteLine();

                    for (uint n = 0; n < NeighDelay.Count(); n++)
                    {
                        var ntok = await ReceiveAsync();
                        Tuple<int, object, int> result = ntok;
                    }
                    //if (TRACE_RECEIVE) Console.WriteLine();
                }
                return Nid;
            }

            private String script { get; set; }
        }

        protected internal class LoyalNode : ByzNode
        {
            protected internal LoyalNode(int nid, int val) : base(nid, val)
            {
            }

            protected internal override async Task<object> Run()
            {
                Init();

                int[] reveiveCount = new int[FaultyCount + 1];
                for (int i = 0; i < FaultyCount + 1; i++)
                {
                    //find all nodes on last level
                    List<EIGNode<int>> lastLevel = tree.root.FindOnLevel(i);
                    //remove the nodes on last level whose label contains process id
                    lastLevel.RemoveAll(c => c.label.Contains(this.Nid.ToString()));

                    foreach (int n in NeighDelay.Keys)
                    {
                        foreach (EIGNode<int> e in lastLevel) e.label = n.ToString();
                        //await 
                        PostAsync(new ByzMessage(i, lastLevel), n, NeighDelay[n]);
                    }

                    while (reveiveCount[i] < NeighDelay.Count())
                    {
                        var ntok = await ReceiveAsync();
                        Tuple<int, object, int> result = ntok;
                        List<EIGNode<int>> updates = (result.Item2 as ByzMessage).nodes;
                        reveiveCount[(result.Item2 as ByzMessage).round]++;
                        foreach (EIGNode<int> u in updates)
                        {
                            String labelTarget = u.label + result.Item1.ToString();
                            EIGNode<int> updateNode = tree.root.Find(labelTarget);
                            if (updateNode != null)
                            {
                                updateNode.value = u.value;
                                //if (Nid == 2) Console.WriteLine("{0}  --   {1}    ----- {2}", labelTarget, u.value, (result.Item2 as ByzMessage).round);
                            }
                        }
                    }
                    //if(TRACE_RECEIVE) Console.WriteLine();
                }
                return Nid;
            }
        }

        // ===========================================================
        // Please do NOT touch this library
        // ===========================================================

        static protected internal int TID() { return Thread.CurrentThread.ManagedThreadId; }

        protected internal class Node
        {
            // static members

            static protected internal bool TRACE_THREAD = true;
            static protected internal bool TRACE_INIT = false;
            static protected internal bool TRACE_POST = false;
            static protected internal bool TRACE_RECEIVE = true;
            static protected internal bool TRACE_PARENT = false;

            static protected internal Dictionary<int, Node> Vertex = new Dictionary<int, Node>();
            static protected internal int EdgeCount = 0;
            static protected internal int MessageCount = 0;

            static protected internal void Reset()
            {
                Vertex = new Dictionary<int, Node>();
                EdgeCount = 0;
                MessageCount = 0;
            }

            static protected internal void Link(int nid1, int nid2, int del12, int del21)
            {
                EdgeCount += 1;
                Vertex[nid1].NeighDelay[nid2] = del12;
                Vertex[nid2].NeighDelay[nid1] = del21;
            }

            // instance members

            protected internal int Parent = 0;
            protected internal int Nid;
            protected internal Dictionary<int, int> NeighDelay = new Dictionary<int, int>();

            protected internal Node(int nid)
            {
                Nid = nid;
                Vertex[nid] = this;
            }

            public override string ToString()
            {
                var ns = NeighDelay.Keys.Aggregate("", (a, n) => a + n + ":" + NeighDelay[n] + ", ");
                return string.Format("<Nid:{0}, Parent:{1}, NeighDelay=({2})>",
                    Nid, Parent, ns);
            }

            protected internal virtual void Init()
            {
                if (TRACE_INIT)
                {
                    if (TRACE_THREAD) Console.Write("[{0}] ", TID());
                    Console.WriteLine("{0}", this);
                }
            }

            protected internal virtual async Task<object> Run()
            {
                return null;
            }

            protected internal BufferBlock<Tuple<int, object, int>> inbox = new BufferBlock<Tuple<int, object, int>>();

            protected internal async Task<bool> PostAsync(object tok, int nid2, int dly2)
            {
                // MessageCount += 1;
                Interlocked.Increment(ref MessageCount);

                if (TRACE_POST)
                {
                    if (TRACE_THREAD) Console.Write("[{0}] ", TID());
                    Console.WriteLine("{0} -> [{1}] ... {2}:{3}", Nid, tok, nid2, dly2);
                }

                await Task.Delay(dly2);
                return Vertex[nid2].inbox.Post(Tuple.Create(Nid, tok, dly2));
            }

            protected internal async Task<Tuple<int, object, int>> ReceiveAsync()
            {
                var ntok = await inbox.ReceiveAsync();

                if (TRACE_RECEIVE)
                {
                    if (TRACE_THREAD) Console.Write("[{0}] ", TID());
                    Console.WriteLine("{0} -> [{1}] -> {2}:{3}", ntok.Item1, ntok.Item2, Nid, ntok.Item3);
                }

                return ntok;
            }
        }
    }
}
// ---