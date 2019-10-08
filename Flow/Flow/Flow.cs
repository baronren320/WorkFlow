using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;

namespace Flow
{
    /// <summary>
    /// 流程核心类
    /// </summary>
    public class Flow
    {
        #region 元素定义

        public delegate TOut Method<out TOut,in TIn>(TIn @in);

        public delegate void WhenMethodChangeEventHandler(string methodName, OwnerAndForm ownerAndForm, object arg);

        public delegate void WhenFlowEndEventHandler(string instanceName);

        public delegate void WhenFlowStartEventHandler(string instanceName, OwnerAndForm ownerAndForm);

        public delegate void WhenErrorEventHandler(string instanceName, Exception errorInfo);

        public delegate string WhenSelectEventHandler(string methodName);

        public event WhenMethodChangeEventHandler WhenMethodChangeEvent;

        public event WhenFlowEndEventHandler WhenFlowEndEvent;

        public event WhenFlowStartEventHandler WhenFlowStartEvent;

        public event WhenErrorEventHandler WhenErrorEvent;

        public event WhenSelectEventHandler WhenSelectEvent;

        /// <summary>
        /// 流程实例进程化空间
        /// </summary>
        public Dictionary<int, string> flowInstructionSequence = new Dictionary<int, string>();

        /// <summary>
        /// 各个流程节点的方法集合
        /// </summary>
        private Dictionary<string, Tuple<Type,object,object>> methods = new Dictionary<string, Tuple<Type, object,object>>();

        /// <summary>
        /// 各个流程节点的表单URL和审批权限账号集合
        /// </summary>
        private Dictionary<string, OwnerAndForm> methodOwnerAndForm = new Dictionary<string, OwnerAndForm>();

        /// <summary>
        /// 流程指令代码的实例集合
        /// </summary>
        private Dictionary<string, flowInstanceStruct> flowInstance = new Dictionary<string, flowInstanceStruct>();

        /// <summary>
        /// 各个流程节点的方法的返回值以及外部输入input的集合
        /// </summary>
        private Dictionary<string, Dictionary<string, object>> memory = new Dictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// 外部输入input
        /// </summary>
        public Dictionary<string, object> input = new Dictionary<string, object>();

        /// <summary>
        /// 工作流持久化存储的xml路径
        /// </summary>
        private readonly string flowPersistenceStorage = AppContext.BaseDirectory + @"\flowPersistenceStorage.xml";

        /// <summary>
        /// 工作流持久化存储操作文档
        /// </summary>
        private XmlDocument instanceXmlDocument = new XmlDocument();

        /// <summary>
        /// 作流持久化存储操作文档单元
        /// </summary>
        private XmlNode instanceXmlNode;

        /// <summary>
        /// 流程实例结构
        /// </summary>
        private struct flowInstanceStruct
        {
            public int flowIndex { get; set; }
            public bool nextStep { get; set; }
            public string startStepTime { get; set; }
        }

        /// <summary>
        /// 条件判断式结构
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        public class SelectCondition<TEntity> where TEntity: ParentDBModel
        {
            public TEntity dbModelData { get; set; }
            public List<Tuple<string, string, string>> condition { get; set; } = new List<Tuple<string, string, string>>();
            public List<bool> logicLinkofAnd { get; set; } = new List<bool>();
        }

        /// <summary>
        /// 数据模型继承源父类
        /// </summary>
        public class ParentDBModel
        {
            public ParentDBModel()=>ModelID = Guid.NewGuid();

            public Guid ModelID { get; set; }
        }

        /// <summary>
        /// 各个流程节点的表单URL和审批权限账号结构与数据模型绑定
        /// </summary>
        public class OwnerAndForm
        {
            public List<string> owner { get; set; } = new List<string>();
            public string formURL { get; set; }

            public OwnerAndForm(ref List<Tuple<string,Type,object>> bindResult, object dbModelData=null)
            {
                if (!(dbModelData is null))
                {
                    List<Tuple<string, Type, object>> _bindResult = new List<Tuple<string, Type, object>>();
                    dbModelData.GetType().GetProperties().ToList().ForEach(a =>
                    {
                        _bindResult.Add(new Tuple<string, Type, object>(a.Name, a.PropertyType, a.GetValue(dbModelData)));
                    });
                    bindResult = _bindResult;
                }
            }
        }

        /// <summary>
        /// 流程指令代码
        /// </summary>
        public const string flowInstruction =
            "[MEMORY_INPUT] select flag of 1;" +
            "[SELECT] CaseRequest.Create [OR] CaseRequest.Overview [REF] select flag of 1 [IS] ok [WHEN] CaseRequest.Create [IS] noOK [WHEN] CaseRequest.Overview;" +
            "CaseRequest.Create:ProjectGeneralInfo.Fill;" +
            "PurchaseDetail.Create;" +
            "Quotation.Create;" +
            "Quotation.Decide;" +
            "Car.Create;" +
            "Budget.Freeze;" +
            "Project.Create;" +
            "[MEMORY_INPUT] select flag of 2;" +
            "[SELECT] ProjectGeneral.Approve [OR] ProjectGeneral.Reject [REF] select flag of 2 [IS] ok [WHEN] ProjectGeneral.Approve [IS] noOK [WHEN] ProjectGeneral.Reject;" +
            "[MEMORY_INPUT] select flag of 3;" +
            "ProjectGeneral.Approve:[SELECT] HigherProjectGeneral.Approve [OR] HigherProjectGeneral.Reject [REF] select flag of 3 [IS] ok [WHEN] HigherProjectGeneral.Approve [IS] noOK [WHEN] HigherProjectGeneral.Reject;" +
            "HigherProjectGeneral.Approve:Budget.Block;" +
            "Contract.Create;" +
            "Contract.Upload;" +
            "[MEMORY_INPUT] select flag of 4;" +
            "[SELECT] Contract.Approve [OR] Contract.Reject [REF] select flag of 4 [IS] ok [WHEN] Contract.Approve [IS] noOK [WHEN] Contract.Reject;" +
            "Contract.Approve:PurchaseOrder.Release;" +
            "PaymentRequest.Fill;" +
            "[MEMORY_INPUT] select flag of 5;" +
            "[SELECT] PaymentRequest.Approve [OR] PaymentRequest.Reject [REF] select flag of 5 [IS] ok [WHEN] PaymentRequest.Approve [IS] noOK [WHEN] PaymentRequest.Reject;" +
            "PaymentRequest.Approve:QDCFinance.Check;" +
            "MidcomFinance.Arrange;" +
            "EstimatedPaymentDate.Fill;" +
            "[JUMP] EndFlow;" +
            "CaseRequest.Overview:CaseRequest.Getall;" +
            "[JUMP] EndFlow;" +
            "ProjectGeneral.Reject:ProjectCreater.Notify;" +
            "[JUMP] EndFlow;" +
            "HigherProjectGeneral.Reject:ProjectCreater.Notify;" +
            "ProjectGeneral.Notify;" +
            "[JUMP] EndFlow;" +
            "Contract.Reject:ProjectCreater.Notify;" +
            "ProjectGeneral.Notify;" +
            "HigherProjectGeneral.Notify;" +
            "[JUMP] EndFlow;" +
            "PaymentRequest.Reject:ProjectCreater.Notify;" +
            "ProjectGeneral.Notify;" +
            "HigherProjectGeneral.Notify;" +
            "EndFlow:Flow.End;";

        #endregion

        #region 流程指令代码相关项

        /// <summary>
        /// 流程实例化
        /// </summary>
        /// <param name="index">不必填写此参数</param>
        public Flow(int index=0)=> flowInstruction.TrimEnd(';').Split(';').ToList().ForEach(a => { flowInstructionSequence.Add(index = index + 1, a + ";"); });

        /// <summary>
        /// 输出流程的指令代码
        /// </summary>
        /// <param name="index">不必填写此参数</param>
        /// <returns>返回流程的指令代码</returns>
        public string OutputFlowCode(int index = 0)
        {
            string result = "";
            flowInstructionSequence.ToList().ForEach(a => { result += "The " + (index += 1).ToString() + " method is \"" + a.Value + "\"\n"; });
            return result;
        }

        #endregion

        #region 流程外部控制

        /// <summary>
        /// 启动流程实例
        /// </summary>
        /// <param name="instanceName">流程实例唯一名称</param>
        public void StartFlow(string instanceName)
        {
            lock (flowInstance)
            {
                if (!flowInstance.ContainsKey(instanceName) && !memory.ContainsKey(instanceName) && !input.ContainsKey(instanceName))
                {
                    instanceXmlDocument.Load(flowPersistenceStorage);
                    int _flowIndex = 1;
                    flowInstanceStruct _flowInstanceStruct= new flowInstanceStruct()
                    {
                        flowIndex = 0,
                        nextStep = false
                    };
                    foreach (XmlNode i in instanceXmlDocument["FlowInstance"].ChildNodes)
                    {
                        if (i.Attributes["Name"].Value == instanceName)
                        {
                            instanceXmlNode = i;
                            _flowInstanceStruct = new flowInstanceStruct()
                            {
                                flowIndex = Convert.ToInt32(i.SelectNodes("FlowIndex")[0].Attributes["Value"].Value),
                                nextStep = false
                            };
                            _flowIndex = _flowInstanceStruct.flowIndex;
                            break;
                        }
                    }
                    flowInstance.Add(instanceName,_flowInstanceStruct);
                    memory.Add(instanceName, new Dictionary<string, object>());
                    input.Add(instanceName, null);
                    WhenFlowStartEvent?.Invoke(instanceName, methodOwnerAndForm.ContainsKey(flowInstructionSequence[_flowIndex].TrimEnd(';')) ? methodOwnerAndForm[flowInstructionSequence[_flowIndex].TrimEnd(';')]:null);
                }
                new Thread(new ParameterizedThreadStart(RunFlow)).Start(instanceName);
            }
        }

        /// <summary>
        /// 流程实例开始下一阶段
        /// </summary>
        /// <param name="instanceName">流程实例唯一名称</param>
        public void NextFlowStep(string instanceName)
        {
            lock (flowInstance)
            {
                if (flowInstance.ContainsKey(instanceName))
                {
                    flowInstanceStruct _flowInstance = flowInstance[instanceName];
                    _flowInstance.nextStep = true;
                    flowInstance[instanceName] = _flowInstance;
                }
            }
        }

        #endregion

        #region 流程内部控制

        private string GetNextFlowStep(ref int nextIndex, string nowFlow = "")
        {
            string result = "";
            if(nowFlow=="") result=flowInstructionSequence[nextIndex = nextIndex + 1];
            else
            {
                for (int i = 1; i<=flowInstructionSequence.Count; i++)
                {
                    if(flowInstructionSequence[i].Contains(nowFlow+":"))
                    {
                        result = flowInstructionSequence[i].Replace(nowFlow+":","").TrimEnd(';');
                        nextIndex = i;
                        break;
                    }
                }
            }
            return result;
        }

        private Dictionary<string, string> Select(string nowFlow)
        {
            if (nowFlow.Contains("[SELECT]"))
            {
                Dictionary<string, string> select = new Dictionary<string, string>();
                nowFlow = nowFlow.Substring(nowFlow.IndexOf("[SELECT]")).Substring("[SELECT]".Length+1);
                string _ref = nowFlow.Substring(nowFlow.IndexOf("[REF]")).Substring("[REF]".Length + 1);
                nowFlow = nowFlow.Replace(_ref, "").Replace("[REF]", "").TrimEnd();
                string temp = "";
                char flag='_';
                foreach (char i in nowFlow)
                {
                    if (i == '[' && flag == '_' ||
                        i == 'O' && flag == '[' ||
                        i == 'R' && flag == 'O' ||
                        i == ']' && flag == 'R') flag = i;
                    else temp += i;
                    if (flag == ']' || i == ';')
                    {
                        select.Add(select.Count.ToString(),temp.Trim().TrimEnd(';')+";");
                        temp = "";
                        flag = '_';
                    }
                }
                select.Add(select.Count.ToString(),temp);
                select.Add(select.Count.ToString(), _ref.Substring(0,_ref.IndexOf("[IS]")).TrimEnd());
                string selectopt = _ref.Replace(select[(select.Count-1).ToString()], "").TrimStart();
                int keyIndex = 1, valueIndex = 3;
                string[] _selectopt = selectopt.Split(' ');
                while(valueIndex< _selectopt.Length)
                {
                    select.Add(_selectopt[keyIndex], _selectopt[valueIndex].TrimEnd(';'));
                    keyIndex += 4;
                    valueIndex += 4;
                }
                return select;
            }
            return null;
        }

        private string Jump(string nowFlow)
        {
            if (nowFlow.StartsWith("[JUMP]")) return nowFlow.Substring("[JUMP]".Length + 1).Trim();
            return null;
        }

        private string MemoryInput(string nowFlow)
        {
            if (nowFlow.StartsWith("[MEMORY_INPUT]")) return nowFlow.Substring("[MEMORY_INPUT]".Length + 1).TrimEnd(';');
            return null;
        }

        #endregion

        #region 流程阶段方法

        /// <summary>
        /// 声明流程的每个阶段的具体方法
        /// </summary>
        /// <typeparam name="TOut">方法的返回类型</typeparam>
        /// <typeparam name="TIn">方法的传入参数类型</typeparam>
        /// <param name="methodName">方法所对应的流程节点的名称</param>
        /// <param name="method">方法的具体实现委托</param>
        /// <param name="ownerAndForm">方法所对应的流程节点的表单和审批权限账号(可选项)</param>
        /// <param name="in">方法的传入参数(可选项)</param>
        public void DeclareMethod<TOut,TIn>(string methodName, Method<TOut, TIn> method, OwnerAndForm ownerAndForm=null,object @in=null)
        {
            if (!(method is null)) if (!methods.ContainsKey(methodName)) methods.Add(methodName, new Tuple<Type, object,object>(typeof(TOut),method,@in));
            if (!(methodOwnerAndForm is null)) if (!methodOwnerAndForm.ContainsKey(methodName)) methodOwnerAndForm.Add(methodName, ownerAndForm);
        }

        /// <summary>
        /// 在指定的方法返回类型范围内调用方法并保存方法的返回值
        /// </summary>
        /// <param name="methodName">方法所对应的流程节点的名称</param>
        /// <param name="instanceName">流程的实例的唯一名称</param>
        private void DoMethod(string methodName, string instanceName)
        {
            if (methods.ContainsKey(methodName))
            {
                if(methods[methodName].Item1.Name is "Int32") memory[instanceName].Add(memory[instanceName].Count.ToString(), (methods[methodName].Item2 as Method<int, object>)?.Invoke(methods[methodName].Item3));
            }
        }

        #endregion

        #region 流程实例运行

        /// <summary>
        /// 按流程指令代码逐条执行，以多线程方式运行多个流程实例
        /// </summary>
        /// <param name="instanceName">流程的实例的唯一名称</param>
        private void RunFlow(object instanceName)
        {
            string _instanceName = instanceName as string, startStepTime;
            int flowIndex = 0;
            bool nextStep = true;
            while (flowIndex < flowInstructionSequence.Count)
            {
                Thread.Sleep(100);
                lock (flowInstance)
                {
                    if (flowInstance.ContainsKey(_instanceName))
                    {
                        flowIndex = flowInstance[_instanceName].flowIndex;
                        nextStep = flowInstance[_instanceName].nextStep;
                        startStepTime = flowInstance[_instanceName].startStepTime;
                    }
                }
                if (!nextStep) continue;
                string nextFlow = GetNextFlowStep(ref flowIndex), _nextFlow = null;
                if (!(WhenSelectEvent is null)) _nextFlow = WhenSelectEvent.Invoke(nextFlow.TrimEnd(';'));
                if (_nextFlow != null) nextFlow = GetNextFlowStep(ref flowIndex, _nextFlow);
                if (nextFlow.StartsWith("[MEMORY_INPUT]"))
                {
                    memory[_instanceName].Add(MemoryInput(nextFlow), input[_instanceName]);
                }
                else if (nextFlow.Contains("[SELECT]"))
                {
                    Dictionary<string, string> select = Select(nextFlow);
                    nextFlow = select[memory[_instanceName][select[(select.Count / 2).ToString()]] as string].TrimEnd(';').Trim();
                    nextFlow = GetNextFlowStep(ref flowIndex, nextFlow);
                }
                else if (nextFlow.Contains("[JUMP]"))
                {
                    nextFlow = Jump(nextFlow).TrimEnd(';');
                    nextFlow = GetNextFlowStep(ref flowIndex, nextFlow);
                }
                if(!(WhenMethodChangeEvent is null))WhenMethodChangeEvent.Invoke(nextFlow.TrimEnd(';'), methodOwnerAndForm.ContainsKey(nextFlow.TrimEnd(';')) ? methodOwnerAndForm[nextFlow.TrimEnd(';')] : null
                     , memory[_instanceName].ContainsKey((memory[_instanceName].Count - 1).ToString()) ? memory[_instanceName][(memory[_instanceName].Count - 1).ToString()] : null);
                try { DoMethod(nextFlow.TrimEnd(';'), _instanceName); } catch(Exception exp) {if(!(WhenErrorEvent is null)) WhenErrorEvent.Invoke(_instanceName, exp); }
                nextStep = false;
                lock (flowInstance)
                {
                    flowInstance[_instanceName] = new flowInstanceStruct()
                    {
                        flowIndex = flowIndex,
                        nextStep = nextStep,
                        startStepTime = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss")
                    };
                    if (instanceXmlNode is null)
                    {
                        XmlElement _elem = instanceXmlDocument.CreateElement("Flow");
                        _elem.SetAttribute("Name", _instanceName);
                        _elem.InnerXml = "<FlowIndex Value='1'/><LastStartStepTime Value='" + flowInstance[_instanceName].startStepTime + "'/>";
                        instanceXmlNode = instanceXmlDocument["FlowInstance"].AppendChild(_elem);
                    }
                    XmlElement elem = instanceXmlDocument.CreateElement("Step");
                    elem.SetAttribute("Name", nextFlow.TrimEnd(';'));
                    elem.SetAttribute("Index", flowIndex.ToString());
                    elem.SetAttribute("StartStepTime", flowInstance[_instanceName].startStepTime);
                    instanceXmlNode.AppendChild(elem);
                    instanceXmlNode.SelectNodes("FlowIndex")[0].Attributes["Value"].Value = flowIndex.ToString();
                    instanceXmlNode.SelectNodes("LastStartStepTime")[0].Attributes["Value"].Value = flowInstance[_instanceName].startStepTime;
                    instanceXmlDocument.Save(flowPersistenceStorage);
                }
            }
            if(!(WhenFlowEndEvent is null))WhenFlowEndEvent.Invoke(_instanceName);
            input.Remove(_instanceName);
            memory.Remove(_instanceName);
            flowInstance.Remove(_instanceName);
            instanceXmlDocument["FlowInstance"].RemoveChild(instanceXmlNode);
            instanceXmlDocument.Save(flowPersistenceStorage);
        }

        #endregion

        #region 条件判断

        /// <summary>
        /// 根据数据模型和条件判断式计算条件判断结果的真假值
        /// </summary>
        /// <typeparam name="TEntity">数据模型的类型</typeparam>
        /// <param name="selectCondition">条件判断式</param>
        /// <returns>条件判断结果的真假值</returns>
        public bool ResultOfSelectCondition<TEntity>(SelectCondition<TEntity> selectCondition) where TEntity : ParentDBModel
        {
            bool result = true;
            int indexOflogicLink = 0;
            selectCondition.logicLinkofAnd.Insert(0, true);
            selectCondition.condition.ForEach(a =>
            {
                typeof(TEntity).GetProperties().ToList().ForEach(b =>
                {
                    if (b.Name == a.Item1)
                    {
                        string value = b.GetValue(selectCondition.dbModelData).ToString();
                        if (a.Item2 is "==" && a.Item3 == value && selectCondition.logicLinkofAnd[indexOflogicLink] && result ||
                            a.Item2 is "==" && a.Item3 != value && !selectCondition.logicLinkofAnd[indexOflogicLink] ||
                            a.Item2 is "==" && a.Item3 == value && !selectCondition.logicLinkofAnd[indexOflogicLink] && !result) result = true;
                        else if (a.Item2 is "!=" && a.Item3 != value && selectCondition.logicLinkofAnd[indexOflogicLink] && result ||
                            a.Item2 is "!=" && a.Item3 == value && !selectCondition.logicLinkofAnd[indexOflogicLink] ||
                            a.Item2 is "!=" && a.Item3 != value && !selectCondition.logicLinkofAnd[indexOflogicLink] && !result) result = true;
                        else result = false;
                    }
                });
                indexOflogicLink++;
            });
            return result;
        }

        #endregion
    }
}
