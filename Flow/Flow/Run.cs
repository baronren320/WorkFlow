using System;
using System.Collections.Generic;

namespace Flow
{
    /// <summary>
    /// 流程实例类
    /// </summary>
    public class Run:Flow
    {
        public class DBModel: ParentDBModel//数据模型
        {
            public string id { get; set; }

            public bool isCar { get; set; }
        }

        public DBModel dbmodeldata = new DBModel() { id = "111", isCar = true };


        public Run()
        {
            WhenMethodChangeEvent += WhenMethodChange;//注册流程阶段变更事件
            WhenFlowStartEvent += WhenFlowStart;//注册流程启动事件
            WhenFlowEndEvent += WhenFlowEnd;//注册流程结束事件
            WhenErrorEvent += WhenError;//注册流程运行中出错事件
            WhenSelectEvent += WhenSelect;//注册流程分支事件
        
        }

        private void WhenFlowStart(string instanceName, OwnerAndForm ownerAndForm)
        {
             
        }

        public void WhenMethodChange(string methodName, OwnerAndForm ownerAndForm, object arg)
        {
           if(!(ownerAndForm is null))
           {
                string formURL = ownerAndForm.formURL;//获取即将进入的流程阶段的表单URL
                List<string> owner = ownerAndForm.owner;//获取即将进入的流程阶段的拥有审批权限的账号
           }
           if(!(arg is null))
           {
                //获取上一个流程阶段的方法的返回值
           }
        }

        public void WhenFlowEnd(string instanceName)
        {
            
        }

        public void WhenError(string instanceName, Exception errorInfo)
        {

        }

        public string WhenSelect(string methodName)
        {
            SelectCondition<DBModel> selectCondition = new SelectCondition<DBModel>();
            //设置判断条件
            selectCondition.condition.Add(new Tuple<string, string, string>("id", "==", "1311"));
            selectCondition.condition.Add(new Tuple<string, string, string>("isCar", "==", "True"));
            //传入数据模型
            selectCondition.dbModelData = dbmodeldata;
            //设置条件间的逻辑连接是And逻辑连接还是OR逻辑连接
            selectCondition.logicLinkofAnd.Add(false);

            if (methodName == "CaseRequest.Getall")
            {
                //获取条件判断的真假结果
                if(ResultOfSelectCondition(selectCondition)) return "ProjectGeneral.Reject";
            } 
            return null;
        }
    }
}
