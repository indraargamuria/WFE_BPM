var vQuery = queryResultDataset.Results.FirstOrDefault(r=>r.RowMod=="U");

var LaborSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.LaborSvcContract>(Db);


string sC = callContextClient.CurrentCompany;
var v = vQuery;

  string iEmpID = v.LaborDtl_EmployeeNum;
  string iJobNum = v.LaborDtl_JobNum;
  int iAsmSeq = 0;
  int iOprSeq = 10;
  DateTime iClockDate = Convert.ToDateTime(v.LaborDtl_ClockInDate);
  //decimal dClockIn = Convert.ToDecimal(v.LaborDtl_ClockinTime);
  //decimal dClockOut = Convert.ToDecimal(v.LaborDtl_ClockOutTime);
  decimal dClockIn = Convert.ToDecimal(8);
  decimal dClockOut = Convert.ToDecimal(17);
  string sRateType = v.Calculated_RateType;

  var rate = (from emp in Db.EmpBasic
              join u in Db.UD07 on emp.xEmployeeGroup_c equals u.Key1
              where emp.EmpID == iEmpID
                 && u.Company == sC
              select new
              {
                  RateR = u.Number01,
                  RateOT = u.Number02,
                  RateDT = u.Number05
              }).FirstOrDefault();

  /*
  if(v.LaborDtl_ClockOutTime > v.LaborDtl_ClockinTime){
    v.LaborDtl_LaborHrs = v.LaborDtl_ClockOutTime - v.LaborDtl_ClockinTime;
  }
  */
  if (rate != null)
  {
      if (v.Calculated_RateType == "R")
          v.Calculated_Rate = rate.RateR;
      else if (v.Calculated_RateType == "OT")
          v.Calculated_Rate = rate.RateOT;
      else if (v.Calculated_RateType == "DT")
          v.Calculated_Rate = rate.RateDT;
      else
          v.Calculated_Rate = 0;
  }
  else {
          v.Calculated_Rate = 0;
  }
  
  decimal dRate = v.Calculated_Rate;
  decimal dHours = v.LaborDtl_LaborHrs;
  v.Calculated_TotalCost = dHours * dRate;
  var LaborSet = new Erp.Tablesets.LaborTableset();
  var laborHed = Db.LaborHed.FirstOrDefault(h => h.Company == Session.CompanyID && h.EmployeeNum == iEmpID && h.PayrollDate == iClockDate);

  if (laborHed != null)
      LaborSvc.GetNewLaborDtlWithHdr(ref LaborSet, iClockDate, 0, iClockDate, 0, laborHed.LaborHedSeq);
  else
      LaborSvc.GetNewLaborDtlNoHdr(ref LaborSet, iEmpID, false, iClockDate, 0, iClockDate, 0);

  string sOutOprSeq = "";
  LaborSvc.DefaultJobNum(ref LaborSet, iJobNum); // <- ganti dari BAQ nanti
  LaborSvc.DefaultAssemblySeq(ref LaborSet, iAsmSeq);
  LaborSvc.ChangeLaborDtlOprSeq(ref LaborSet, iOprSeq, out sOutOprSeq); // <- ganti dari BAQ nanti


  var LaborDtlRow = LaborSet.LaborDtl[0];
  LaborDtlRow.ClockInDate = iClockDate;
  
  decimal decClockIn = dClockIn;   // e.g., 9.80
  decimal decClockOut = dClockOut;    // e.g., 17.25
  
  LaborSvc.ChangeLaborDtlTimeField("ClockinTime", decClockIn, ref LaborSet);
  LaborSvc.ChangeLaborDtlTimeField("ClockOutTime", decClockOut, ref LaborSet);
  
  
  LaborDtlRow.TimeAutoSubmit = true;

  //LaborSvc.LaborRateCalc(ref LaborSet);
  LaborDtlRow.LaborRate = dRate;
  LaborDtlRow.LaborHrs = dHours;
  LaborDtlRow.BurdenHrs = dHours;
  LaborDtlRow["xRateType_c"] = sRateType;
  LaborDtlRow["xRate_c"] = dRate;
  
  LaborSvc.Update(ref LaborSet);
  
  //Auto Approve
  var LaborApvRow = Epicor.Data.BufferCopy.Copy(LaborDtlRow);
  LaborApvRow.NewDifDateFlag = 2;
  LaborApvRow.RowMod = "U";
  LaborApvRow.RowSelected = true;
  LaborSet.LaborDtl.Add(LaborApvRow);
  string sOutApprove = "";
  LaborSvc.SubmitForApprovalBySelected(ref LaborSet, false, out sOutApprove);
  