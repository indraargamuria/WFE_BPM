var vQuery = queryResultDataset.Results.FirstOrDefault();
var v = vQuery;
string sC = callContextClient.CurrentCompany;

//v.RowMod = v.RowMod == "A" ? "A" : "U";
if (!string.IsNullOrEmpty(v.LaborDtl_EmployeeNum) && !string.IsNullOrEmpty(v.Calculated_RateType))
{
    var rate = (from emp in Db.EmpBasic
                join u in Db.UD07 on emp.xEmployeeGroup_c equals u.Key1
                where emp.EmpID == v.LaborDtl_EmployeeNum
                   && u.Company == sC
                select new
                {
                    RateR = u.Number01,
                    RateOT = u.Number02,
                    RateDT = u.Number05
                }).FirstOrDefault();

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
}

if(v.LaborDtl_LaborHrs == (v.LaborDtl_ClockOutTime - v.LaborDtl_ClockinTime)){
  if(v.LaborDtl_ClockOutTime > v.LaborDtl_ClockinTime){
    v.LaborDtl_LaborHrs = v.LaborDtl_ClockOutTime - v.LaborDtl_ClockinTime;
  }
  else {
    v.LaborDtl_LaborHrs = 0;
  }
}
else {
    v.LaborDtl_LaborHrs = v.LaborDtl_LaborHrs;
}
v.Calculated_TotalCost = v.LaborDtl_LaborHrs * v.Calculated_Rate;
v.Calculated_TotalBurdenCost = v.LaborDtl_LaborHrs * v.LaborDtl_BurdenRate;
v.Calculated_GrandTotalCost = v.Calculated_TotalCost + v.Calculated_TotalBurdenCost;

v.SetRowState(IceRowState.Added);