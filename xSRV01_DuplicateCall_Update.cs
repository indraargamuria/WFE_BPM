Erp.Tablesets.ServiceCallCenterTableset cExistingCallSet = new Erp.Tablesets.ServiceCallCenterTableset();
Erp.Tablesets.ServiceCallCenterTableset cNewCallSet = new Erp.Tablesets.ServiceCallCenterTableset();
Erp.Tablesets.ServiceCallCenterTableset cNewCallJobSet = new Erp.Tablesets.ServiceCallCenterTableset();
Erp.Contracts.ServiceCallCenterSvcContract cCallSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.ServiceCallCenterSvcContract>(Db);

Erp.Tablesets.JobEntryTableset cJobSet = new Erp.Tablesets.JobEntryTableset();
Erp.Tablesets.JobEntryTableset cNewJobSet = new Erp.Tablesets.JobEntryTableset();
Erp.Contracts.JobEntrySvcContract cJobSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.JobEntrySvcContract>(Db);


try {
  string vCompany = callContextClient.CurrentCompany;
  string vN = Environment.NewLine;
  string vSO = "";
  int vCallNum = queryResultDataset.Results.FirstOrDefault().FSCallhd_CallNum;
  cExistingCallSet = cCallSvc.GetByID(vCallNum);
  
  
  var vExistingCallHead = cExistingCallSet.FSCallhd[0];
  var vExistingCallDetail = cExistingCallSet.FSCallDt;
  
  vExistingCallHead.CallNum = 0;
  vExistingCallHead.OpenCall = true;
  vExistingCallHead.Invoiced = false;
  vExistingCallHead["xRevisionNo_c"] = "R0";
  vExistingCallHead["xState_c"] = vExistingCallHead.CustOnCreditHold == true ? "Hold" : "Draft";
  vExistingCallHead.RowMod = "A";
  
  var vDraftCallHead = Epicor.Data.BufferCopy.Copy(vExistingCallHead);
  
  cNewCallSet.FSCallhd.Add(vDraftCallHead);
  
  cCallSvc.Update(ref cNewCallSet);
  int vCreatedCallNum = cNewCallSet.FSCallhd.FirstOrDefault().CallNum;
  //cL = Copy Line
  foreach(var cL in vExistingCallDetail){
    cL.CallNum = vCreatedCallNum;
    cL.CallLine = 0;
    cL.JobNum = "";
    cL.Invoiced = false;
    cL["xMarginPercentage_c"] = Convert.ToDecimal(0);
    cL["xMobileAppClosed_c"] = false;
    cL.RowMod = "A";
    
    var vDraftCallLine = Epicor.Data.BufferCopy.Copy(cL);
    
    cNewCallSet.FSCallDt.Add(vDraftCallLine);
    //cNewCallSet.FSCallDt.Clear();
    
    
    
    //PublishInfoMessage("Duplicate Line " + cL.CallLine.ToString(), Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
    
  }
  cCallSvc.Update(ref cNewCallSet);
  
  //cLJ = Copy Line Job
  
  foreach(var cLJ in cNewCallSet.FSCallDt){
    cCallSvc.CreateServiceCallJob(cLJ.CallNum, cLJ.CallLine, out vSO, ref cNewCallJobSet);
   
    
  }
  
  foreach(var cLWJ in cNewCallJobSet.FSCallDt){
    
    string vJob = cLWJ.JobNum;
    cJobSet = cJobSvc.GetByID(vJob);
    var vJobHead = cJobSet.JobHead[0];
    vJobHead.JobReleased = true;
    //vJobHead.RowMod = "U";
    vJobHead.RowMod = "U";
    cJobSvc.ChangeJobHeadJobReleased(ref cJobSet);
    vJobHead.PersonID = callContextClient.CurrentUserId;
    vJobHead.RowMod = "U";
    cJobSvc.Update(ref cJobSet);
  }
  
  int vIndex = 0;
  
  cExistingCallSet = cCallSvc.GetByID(vCallNum);
  vExistingCallDetail = cExistingCallSet.FSCallDt;
  foreach(var cLWJ in cNewCallJobSet.FSCallDt){
    string vSourceJob = vExistingCallDetail[vIndex].JobNum;
    string vDestinationJob = cLWJ.JobNum;
    
    //string vJob = cLWJ.JobNum;
    cJobSet = cJobSvc.GetByID(vSourceJob);
    
    
    foreach(var cO in cJobSet.JobOper){
    
      int iAsmSeq = 0;
      string iOpCode = "SERVICE";
      cJobSvc.GetNewJobOper(ref cNewJobSet, vDestinationJob, iAsmSeq);
      cJobSvc.ChangeJobOperOpCode(cO.OpCode, out vSO, ref cNewJobSet);
      var iJobOper = cNewJobSet.JobOper[0];
      iJobOper.OpDesc = cO.OpDesc;
      iJobOper.Billable = false;
      cJobSvc.Update(ref cNewJobSet);
    }
    
    foreach(var cM in cJobSet.JobMtl){
      cM.JobNum = vDestinationJob;
      cM.IssuedQty = 0;
      cM.IssuedComplete = false;
      cM.RowMod = "A";
      
      var vDraftMtl = Epicor.Data.BufferCopy.Copy(cM);
      
      cNewJobSet.JobMtl.Add(vDraftMtl);
      cJobSvc.Update(ref cNewJobSet);
    }
    
  
    vIndex = vIndex + 1;
    
  }
  
  
  string iInfoMessage = 
  "Duplicate Success, New Call Created, No " + cNewCallSet.FSCallhd.FirstOrDefault().CallNum.ToString() + vN
  ;
  
  callContextBpmData.Number19 = vCreatedCallNum;
  PublishInfoMessage(iInfoMessage, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
}
catch(Exception e){
  
  PublishInfoMessage(e.Message, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
}