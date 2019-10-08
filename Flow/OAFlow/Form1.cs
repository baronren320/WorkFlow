using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static Flow.Flow;

namespace OAFlow
{
    public partial class Form1 : Form
    {
        Flow.Run exe = new Flow.Run();

        string instanceName = "test";

        public Form1()
        {
            InitializeComponent();
            //输入数据模型，获取数据模型的每个属性的类型、名称等信息
            List<Tuple<string, Type, object>> bindResult = new List<Tuple<string, Type, object>>();
            Flow.Flow.OwnerAndForm ok = new Flow.Flow.OwnerAndForm(ref bindResult,exe.dbmodeldata);
            //声明每个节点拥有审批权限的账号(可多个)
            ok.owner.Add("dept_mgr");
            //声明每个节点所需的表单的URL地址(只能一个)
            ok.formURL = "~/ProjectGeneralInfo.aspx";
            //声明每个流程节点的具体方法，参数为：节点名称、方法名、权限和表单(可选)、输入参数(可选)
            exe.DeclareMethod<int,object>("CaseRequest.Getall", Doing2);
            exe.DeclareMethod<int, object>("ProjectGeneralInfo.Fill", Doing3,ok);
            exe.DeclareMethod<int, object>("PurchaseDetail.Create", Doing4);
            exe.DeclareMethod<int, object>("Quotation.Create", Doing5);
            exe.DeclareMethod<int, object>("Flow.End", EndFlow);

        }

        

        private void Form1_Load(object sender, EventArgs e)
        {

            // string work_flow =exe.OutputFlowCode();//输出流程的指令代码

        }

        public int Doing(object input)
        {
            MessageBox.Show("CaseRequest.Overview");
            return 100;
        }

        public int Doing2(object input)
        {
            MessageBox.Show("CaseRequest.Getall");
            return 100;
        }

        public int Doing3(object input)
        {
            MessageBox.Show("ProjectGeneralInfo.Fill");
            return 100;
        }
        
        public int Doing4(object input)
        {
            MessageBox.Show("PurchaseDetail.Create");
            return 100;
        }

        public int Doing5(object input)
        {
            MessageBox.Show("Quotation.Create"); 
            return 100;
        }

        public int EndFlow(object input)
        {
            MessageBox.Show("End");
            return 100;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            exe.StartFlow(instanceName);//启动流程实例
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            //根据UI选择设定input的值用于控制流程分支往哪个分支走
            if (radioButton1.Checked) exe.input[instanceName] = "noOK";//分支1
            else exe.input[instanceName] = "ok";//分支2
        }

        private void button2_Click(object sender, EventArgs e)
        {
            exe.NextFlowStep(instanceName);//流程的下一个节点
        }

        
    }
}
