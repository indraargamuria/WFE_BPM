var sModSet = queryResultDataset.Results.FirstOrDefault();

string iPartNum = sModSet.JobMtl_PartNum;
string iCustID = callContextBpmData.ShortChar02;
string iPrevPartNum = callContextBpmData.ShortChar03;
string sCompany = callContextClient.CurrentCompany;

var sDbPart = Db.Part.Where(r=>r.Company==sCompany&&r.PartNum==iPartNum).FirstOrDefault();



if(sModSet.JobMtl_PartNum != "" && sDbPart != null){
  sModSet.JobMtl_Description = sDbPart.PartDescription;
  sModSet.Part_TrackLots = sDbPart.TrackLots;
  sModSet.JobMtl_IUM = sDbPart.IUM;
  try {
    if(iPrevPartNum != iPartNum){
  
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
      DataSet dsResults = dynamicQuery.ExecuteByID("xSRV01_NewMaterialPrice", dsQueryExecution);
      
      string iListCode = "-";
      decimal iListPrice = 0;
      
    
      var sBAQMaterialPrice = dsResults.Tables["Results"].Select().AsEnumerable().FirstOrDefault();
      
      iListCode = sBAQMaterialPrice["CustomerPriceLst_ListCode"].ToString();
      iListPrice = Convert.ToDecimal(sBAQMaterialPrice["PriceLstParts_BasePrice"]);
      
      
      //PublishInfoMessage("Get Price List " + iPartNum + "-" + iPrevPartNum, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
      
      sModSet.JobMtl_DocUnitPrice = iListPrice;
      sModSet.JobMtl_xListCode_c = iListCode;
    }
  }
  catch(Exception e){
  }

}