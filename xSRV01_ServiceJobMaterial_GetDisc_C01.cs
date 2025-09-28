var sModSet = queryResultDataset.Results.FirstOrDefault();
string iCustID = callContextBpmData.ShortChar02;
string iPartNum = sModSet.JobMtl_PartNum;
decimal iQty = sModSet.JobMtl_QtyPer;
decimal iPrice = sModSet.JobMtl_DocUnitPrice;

if(sModSet.JobMtl_PartNum != "" && iQty != 0){

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
  paramRow3.ParameterID = "SetQty";
  paramRow3.ParameterValue = iQty.ToString();
  paramRow3.ValueType = "decimal";
  paramRow3.IsEmpty = false;
  dsQueryExecution.ExecutionParameter.Add(paramRow3);  
  DataSet dsResults = dynamicQuery.ExecuteByID("xSRV01_NewMaterialDiscount", dsQueryExecution);
  
  decimal iBreakDiscPercent = 0;
  callContextBpmData.Number01 = 0;
  

  var sBAQMaterialPrice = dsResults.Tables["Results"].Select().AsEnumerable().FirstOrDefault();
  
  
  if(sBAQMaterialPrice!=null){
    iBreakDiscPercent = Convert.ToDecimal(sBAQMaterialPrice["PLPartBrk_DiscountPercent"]);
    sModSet.Calculated_Discount = iBreakDiscPercent;
    callContextBpmData.Number01 = iBreakDiscPercent;
    //PublishInfoMessage("Tf is This " + iPartNum, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
      
  }
  else {
    sModSet.Calculated_Discount = 0;
    callContextBpmData.Number01 = 0;
  }
  
  
}
else {
  sModSet.Calculated_Discount = 0;
  callContextBpmData.Number01 = 0;
}