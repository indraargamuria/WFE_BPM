
Erp.Tablesets.ServiceCallCenterTableset sCallSet = new Erp.Tablesets.ServiceCallCenterTableset();
Erp.Contracts.ServiceCallCenterSvcContract sCallSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.ServiceCallCenterSvcContract>(Db);
Erp.Tablesets.JobEntryTableset sJobSet = new Erp.Tablesets.JobEntryTableset();
Erp.Contracts.JobEntrySvcContract sJobSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.JobEntrySvcContract>(Db);

string sCompany = callContextClient.CurrentCompany;
string sOutMsg = "";
var iView = queryResultDataset.Results.FirstOrDefault();

if(iView != null){

  int iCallNum = iView.FSCallDt_CallNum;
  int iCallLine = iView.FSCallDt_CallLine;
  
  var dFSCallDt = Db.FSCallDt.FirstOrDefault(r => r.Company == sCompany && r.CallNum == iCallNum && r.CallLine == iCallLine);
  
  if(dFSCallDt.JobNum == ""){
    try {
      sCallSvc.CreateServiceCallJob(iCallNum, iCallLine, out sOutMsg, ref sCallSet);
      
      dFSCallDt = Db.FSCallDt.FirstOrDefault(r => r.Company == sCompany && r.CallNum == iCallNum && r.CallLine == iCallLine);
      
      string iJobNum = dFSCallDt.JobNum;
      int iAsmSeq = 0;
      string iOpCode = "SERVICE";
      
      sJobSet = sJobSvc.GetByID(iJobNum);
      var iJobHead = sJobSet.JobHead[0];
      iJobHead.JobEngineered = true;
      iJobHead.JobReleased = true;
      iJobHead.InCopyList = true;
      iJobHead.StartDate = DateTime.Today;
      iJobHead.DueDate = DateTime.Today;
      iJobHead.PersonID = callContextClient.CurrentUserId;
      iJobHead.RowMod = "U";
      sJobSvc.Update(ref sJobSet);
      
      //PublishInfoMessage("Job " + iJobNum + " Created", Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
      
      sJobSvc.GetNewJobOper(ref sJobSet, iJobNum, iAsmSeq);
      sJobSvc.ChangeJobOperOpCode(iOpCode, out sOutMsg, ref sJobSet);
      var iJobOper = sJobSet.JobOper[0];
      iJobOper.OpDesc = "Default Operation";
      iJobOper.Billable = false;
      sJobSvc.Update(ref sJobSet);
    }
    catch(Exception e){
      PublishInfoMessage("Create Job Failed, Reason: " + e.Message, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
    }
  }

}