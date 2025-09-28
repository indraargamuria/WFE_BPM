var sModSet = queryResultDataset.Results.FirstOrDefault();

string iPartNum = sModSet.JobMtl_PartNum;
decimal iPrice = sModSet.JobMtl_DocUnitPrice;
string iCustID = callContextBpmData.ShortChar02;
string iPrevPartNum = callContextBpmData.ShortChar03;
string sCompany = callContextClient.CurrentCompany;
decimal iDiscount = callContextBpmData.Number01;

var sDbPart = Db.Part.Where(r=>r.Company==sCompany&&r.PartNum==iPartNum).FirstOrDefault();



if(sModSet.JobMtl_PartNum != "" && sDbPart != null){
  try {
  
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
      
      iListPrice = Convert.ToDecimal(sBAQMaterialPrice["PriceLstParts_BasePrice"]);
      
      
      //PublishInfoMessage("Get Price List " + iPartNum + "-" + iPrevPartNum, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
      if(iPrice != 0 && iDiscount != 0){
        sModSet.JobMtl_DocUnitPrice = iListPrice - (iDiscount/100*iListPrice);
      }
      else {
        sModSet.JobMtl_DocUnitPrice = iListPrice - (iDiscount/100*iListPrice);
        sModSet.JobMtl_DocUnitPrice = iListPrice;
      }
      callContextBpmData.Number01 = 0;
  }
  catch(Exception e){
  }

}