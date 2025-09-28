// Filter updated records from the dataset
var iUpdatedSet = queryResultDataset.Results.Where(r => r.RowMod == "U");

// Initialize Service Call BO
Erp.Tablesets.ServiceCallCenterTableset sCallSet = new Erp.Tablesets.ServiceCallCenterTableset();
Erp.Contracts.ServiceCallCenterSvcContract sCallSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.ServiceCallCenterSvcContract>(Db);
Erp.Tablesets.ARTrackerTotTableset sAgingSet = new Erp.Tablesets.ARTrackerTotTableset();
Erp.Contracts.ARAgingTrackerSvcContract sAgingSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.ARAgingTrackerSvcContract>(Db);
Erp.Tablesets.JobEntryTableset sJobSet = new Erp.Tablesets.JobEntryTableset();
Erp.Contracts.JobEntrySvcContract sJobSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.JobEntrySvcContract>(Db);

string sCompany = callContextClient.CurrentCompany;
string sPlant = callContextClient.CurrentPlant;
string sOutMsg = "";

// Loop through each updated row
foreach (var i in iUpdatedSet)
{
  string iVehicleNum = i.Calculated_VehicleNum;
  string iPartNum = i.Calculated_PartNum;
  string iDriverContact = i.Calculated_DriverContact;

  // Retrieve Part, Lot, and Customer Data
  var dPart = Db.Part.FirstOrDefault(r => r.Company == sCompany && r.PartNum == iPartNum);
  var dPartLot = Db.PartLot.FirstOrDefault(r => r.Company == sCompany && r.PartNum == iPartNum && r.xVehicleNum_c == iVehicleNum);

  string iCustID = dPartLot.xCustomerID_c;
  var dCustomer = Db.Customer.FirstOrDefault(r => r.Company == sCompany && r.CustID == iCustID);
  string iLineDesc = dPart?.PartDescription ?? "";
  
  var vDbCallCode = Db.FSCallCd.FirstOrDefault(r => r.Company == sCompany);
  
  string iCallType = vDbCallCode.CallCode;
  string iTopic = "MAINT";
  string iLotNum = dPartLot?.LotNum ?? "";
  string iChassisNum = dPartLot?.xChassisNum_c ?? "";
  string iVehicleArrival = i.Calculated_VehicleArrival;

  // Create new Call Header
  sCallSvc.GetNewFSCallhd(ref sCallSet);
  sCallSvc.ChangeHdrCustID(iCustID, ref sCallSet);
  sCallSvc.ChangeHdrCallCode(iCallType, ref sCallSet);

  var iHeaderSet = sCallSet.FSCallhd[0];
  iHeaderSet.Company = sCompany;
  iHeaderSet.Plant = sPlant;
  iHeaderSet["xVehicleArrival_c"] = iVehicleArrival;
  iHeaderSet["xBillingManager_c"] = "sclew";
  sCallSvc.Update(ref sCallSet);


  
  Ice.Contracts.DynamicQuerySvcContract dynamicQuery = Ice.Assemblies.ServiceRenderer.GetService<Ice.Contracts.DynamicQuerySvcContract>(Db);
  Ice.Tablesets.QueryExecutionTableset dsQueryExecution = new QueryExecutionTableset();  
  ExecutionParameterRow paramRow1 = new ExecutionParameterRow();
  paramRow1.ParameterID = "SetCustID";
  paramRow1.ParameterValue = iCustID;
  paramRow1.ValueType = "nvarchar";
  paramRow1.IsEmpty = false;
  dsQueryExecution.ExecutionParameter.Add(paramRow1);  
  ExecutionParameterRow paramRow2 = new ExecutionParameterRow();
  paramRow2.ParameterID = "SetPartNum";
  paramRow2.ParameterValue = iPartNum;
  paramRow2.ValueType = "nvarchar";
  paramRow2.IsEmpty = false;
  dsQueryExecution.ExecutionParameter.Add(paramRow2);  
  ExecutionParameterRow paramRow3 = new ExecutionParameterRow();
  paramRow3.ParameterID = "SetVehicleNum";
  paramRow3.ParameterValue = iVehicleNum;
  paramRow3.ValueType = "nvarchar";
  paramRow3.IsEmpty = false;
  dsQueryExecution.ExecutionParameter.Add(paramRow3);  
  ExecutionParameterRow paramRow4 = new ExecutionParameterRow();
  paramRow4.ParameterID = "SetLotNum";
  paramRow4.ParameterValue = iLotNum;
  paramRow4.ValueType = "nvarchar";
  paramRow4.IsEmpty = false;
  dsQueryExecution.ExecutionParameter.Add(paramRow4);  
  DataSet dsResults = dynamicQuery.ExecuteByID("xSRV01_ValidateActiveContract", dsQueryExecution);
  
  bool iHasActiveServiceContract = false;
  
  if (dsResults.Tables[0].Rows.Count > 0)
  {
    var sResult = dsResults.Tables["Results"].Select().AsEnumerable().FirstOrDefault();
    
    iHasActiveServiceContract = Convert.ToBoolean(sResult["Calculated_HasActiveContract"]);
  
  
  }
  

  // Create new Call Detail
  sCallSvc.GetNewFSCallDt(ref sCallSet, iHeaderSet.CallNum);
  sCallSvc.ChangeDtlPartNum(ref sCallSet, iPartNum);

  var iLineSet = sCallSet.FSCallDt[0];
  iLineSet.PartNum = iPartNum;
  iLineSet.LineDesc = iLineDesc;
  iLineSet.PartDescription = "General Maintenance - Recorded from Mobile App";
  iLineSet.IssueTopicID1 = iTopic;
  iLineSet.CallQty = 1;

  // Assign UD fields from PartLot
  iLineSet.SetUDField<string>("xVehicleNum_c", iVehicleNum);
  iLineSet.SetUDField<string>("xLotNum_c", iLotNum);
  iLineSet.SetUDField<string>("xDriverContact_c", iDriverContact);
  iLineSet.SetUDField<bool>("xHasServiceContract_c", iHasActiveServiceContract);
  iLineSet.SetUDField<string>("xOriginalVehicleNum_c", dPartLot?.xOriginalVehicleNum_c ?? "");
  iLineSet.SetUDField<string>("xChassisNum_c", dPartLot?.xChassisNum_c ?? "");
  iLineSet.SetUDField<string>("xChassisNum_c", dPartLot?.xChassisNum_c ?? "");
  iLineSet.SetUDField<string>("xFactorySpec_c", dPartLot?.xFactorySpec_c ?? "");
  iLineSet.SetUDField<string>("xCustomerID_c", dPartLot?.xCustomerID_c ?? "");
  iLineSet.SetUDField<string>("xCustomerSpec_c", dPartLot?.xCustomerSpec_c ?? "");
  iLineSet.SetUDField<string>("xUnitRemark_c", dPartLot?.xUnitRemark_c ?? "");
  iLineSet.SetUDField<string>("xSNRemark_c", dPartLot?.xSNRemark_c ?? "");
  iLineSet.SetUDField<DateTime?>("xWarrantyBeginDate_c", dPartLot?.xWarrantyBeginDate_c);
  iLineSet.SetUDField<DateTime?>("xWarrantyEndDate_c", dPartLot?.xWarrantyEndDate_c);
  iLineSet.SetUDField<DateTime?>("xCOERegistrationDate_c", dPartLot?.xCOERegistrationDate_c);
  iLineSet.SetUDField<DateTime?>("xCOEExpiryDate_c", dPartLot?.xCOEExpiryDate_c);
  iLineSet.SetUDField<DateTime?>("xCOEUpdateDate_c", dPartLot?.xCOEUpdateDate_c);
  iLineSet.SetUDField<string>("xCOEManufacturer_c", dPartLot?.xCOEManufacturer_c ?? "");

  sCallSvc.Update(ref sCallSet);

  // Generate service job from detail
  sCallSvc.CreateServiceCallJob(iHeaderSet.CallNum, iLineSet.CallLine, out sOutMsg, ref sCallSet);

  var dFSCallDt = Db.FSCallDt.FirstOrDefault(r => r.Company == sCompany && r.CallNum == iHeaderSet.CallNum && r.CallLine == iLineSet.CallLine);

  // Prepare response values
  int rCallNum = iHeaderSet.CallNum;
  string rLotNum = iLotNum;
  string rJobNum = dFSCallDt.JobNum;
  string rCustName = dCustomer.Name;
  bool rCreditHold = dCustomer.CreditHold;
  DateTime? rWarrantyBegin = dPartLot?.xWarrantyBeginDate_c;
  DateTime? rWarrantyEnd = dPartLot?.xWarrantyEndDate_c;
  bool rHasActiveContract = iHasActiveServiceContract;
  string rDriverContact = iDriverContact;
  
  sJobSet = sJobSvc.GetByID(rJobNum);
  var iJobHead = sJobSet.JobHead[0];
  iJobHead.JobReleased = true;
  iJobHead.RowMod = "U";
  sJobSvc.Update(ref sJobSet);

  
  int iAsmSeq = 0;
  string iOpCode = "SERVICE";
  sJobSvc.GetNewJobOper(ref sJobSet, rJobNum, iAsmSeq);
  sJobSvc.ChangeJobOperOpCode(iOpCode, out sOutMsg, ref sJobSet);
  var iJobOper = sJobSet.JobOper[0];
  iJobOper.OpDesc = "Default Operation";
  iJobOper.Billable = false;
  sJobSvc.Update(ref sJobSet);
  
  // Call ARAgingTrackerSvc to get Due amounts
  sAgingSet = sAgingSvc.GenerateTracker(iCustID);
  var iCustAging = sAgingSet.ARTrackerTot.FirstOrDefault();
  

  // Set calculated fields for response
  i.Calculated_CustID = iCustID;
  i.Calculated_CallNum = rCallNum;
  i.Calculated_JobNum = rJobNum;
  i.Calculated_LotNum = rLotNum;
  i.Calculated_DriverContact = rDriverContact;
  i.Calculated_CustName = rCustName;
  i.Calculated_CreditHold = rCreditHold;
  i.Calculated_WarrantyBegin = rWarrantyBegin;
  i.Calculated_WarrantyEnd = rWarrantyEnd;
  i.Calculated_HasServiceContract = rHasActiveContract;
  i.Calculated_AgingDueCurrent = iCustAging.CurrDueAmt;
  i.Calculated_AgingDue30 = iCustAging.Due30Amt;
  i.Calculated_AgingDue60 = iCustAging.Due60Amt;
  i.Calculated_AgingDue90 = iCustAging.Due90Amt;
  i.Calculated_AgingDue120 = iCustAging.Due120Amt;
  i.Calculated_AgingDueFuture = iCustAging.FutureDueAmt;
  i.RowMod = "U";
}
