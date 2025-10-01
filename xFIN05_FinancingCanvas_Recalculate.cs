var row = queryResultDataset.Results.FirstOrDefault();
if (row == null) return;

int orderNum = row.OrderHed_OrderNum;
string company = callContextClient.CurrentCompany;

var dbOrder = Db.OrderHed.FirstOrDefault(r => r.Company == company && r.OrderNum == orderNum);
if (dbOrder == null) return;

var orderSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.SalesOrderSvcContract>(Db);
var orderSet = new Erp.Tablesets.SalesOrderTableset();

bool oldTaxPaidByCustomer = dbOrder.xTaxPaidByCustomer_c;
bool newTaxPaidByCustomer = row.OrderHed_xTaxPaidByCustomer_c;

string oldFinancier = dbOrder.xFinanceCompany_c;
string newFinancier = row.OrderHed_xFinanceCompany_c;

decimal oldFinanceAmt = dbOrder.xFinanceAmt_c;
decimal newFinanceAmt = row.OrderHed_xFinanceAmt_c;

decimal oldDownPayAmt = dbOrder.xDownPayAmt_c;
decimal newDownPayAmt = row.OrderHed_xDownPayAmt_c;

decimal oldTaxableAmt = dbOrder.xManualTaxableAmt_c;
decimal newTaxableAmt = row.OrderHed_xManualTaxableAmt_c;

decimal oldTaxAmt = dbOrder.xManualTaxAmt_c;
decimal newTaxAmt = row.OrderHed_xManualTaxAmt_c;

string oldDownPayRef = dbOrder.xDownPayRef_c;
string newDownPayRef = row.OrderHed_xDownPayRef_c;

bool oldTaxFlag = dbOrder.xManualTax_c;
bool newTaxFlag = row.OrderHed_xManualTax_c;

Action updateOrder = () =>
{
    orderSet = orderSvc.GetByID(orderNum);
    var hdr = orderSet.OrderHed[0];
    hdr["xFinanceCompany_c"] = row.OrderHed_xFinanceCompany_c;
    hdr["xFinanceAmt_c"] = row.OrderHed_xFinanceAmt_c;
    hdr["xManualTax_c"] = row.OrderHed_xManualTax_c;
    hdr["xManualTaxableAmt_c"] = row.OrderHed_xManualTaxableAmt_c;
    hdr["xManualTaxAmt_c"] = row.OrderHed_xManualTaxAmt_c;
    hdr["xDownPayAmt_c"] = row.OrderHed_xDownPayAmt_c;
    hdr["xDownPayRef_c"] = row.OrderHed_xDownPayRef_c;
    hdr["xTaxPaidByCustomer_c"] = row.OrderHed_xTaxPaidByCustomer_c;
    hdr.RowMod = "U";
    orderSvc.Update(ref orderSet);
    
    //PublishInfoMessage("Kamu Modify SO No: " + orderNum.ToString() + " Tax Amt: " + row.OrderHed_xManualTaxAmt_c.ToString() + " Taxable Amt: " + row.OrderHed_xManualTaxableAmt_c.ToString(), Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
};

Func<decimal, decimal> round = val => Math.Round(val, 2);

if (oldFinancier != newFinancier)
{
    updateOrder();
}
else if (oldTaxPaidByCustomer != newTaxPaidByCustomer)
{
    updateOrder();
}
else if (oldDownPayAmt != newDownPayAmt)
{
    var customerPayable = row.UD40_Number08;
    var netPayable = customerPayable - newDownPayAmt;
    row.UD40_Number11 = round(netPayable);
    updateOrder();
}
else if (oldTaxFlag != newTaxFlag && newTaxFlag == true)
{
    row.OrderHed_xManualTax_c = true;
    updateOrder();
}
else if (newTaxFlag == true && newTaxAmt == 0 && newTaxableAmt == 0)
{
    // New logic block for manual tax with 0 values
    row.OrderHed_xManualTaxAmt_c = 0;
    row.OrderHed_xManualTaxableAmt_c = 0;

    var taxableTotal = row.UD40_Number03 + row.OrderHed_xManualTaxableAmt_c;
    var taxTotal = row.UD40_Number05 + row.OrderHed_xManualTaxAmt_c;
    var totalWithTax = row.OrderHed_DocTotalCharges + taxTotal;
    var payableCustomer = row.UD40_Number03 + row.UD40_Number05 + row.OrderHed_xManualTaxAmt_c;
    var payableTotal = payableCustomer + row.UD40_Number09;
    var netPayable = payableCustomer - row.OrderHed_xDownPayAmt_c;

    row.UD40_Number04 = round(taxableTotal);
    row.UD40_Number06 = round(taxTotal);
    row.UD40_Number07 = round(totalWithTax);
    row.UD40_Number08 = round(payableCustomer);
    row.UD40_Number10 = round(payableTotal);
    row.UD40_Number11 = round(netPayable);

    updateOrder();
}
else if (oldDownPayRef != newDownPayRef)
{
    updateOrder();
}
else if (oldTaxFlag != newTaxFlag && newTaxFlag == false)
{
    row.OrderHed_xManualTax_c = false;

    var totalCharge = row.OrderHed_DocTotalCharges;
    var financeAmt = row.OrderHed_xFinanceAmt_c;
    var customerAmt = totalCharge - financeAmt;

    var taxableCustomer = customerAmt;
    var taxableFinance = financeAmt;
    var taxableTotal = taxableCustomer + taxableFinance;

    var taxRate = row.Calculated_DocTaxPercentage;

    var taxCustomer = taxableCustomer * taxRate / 100;
    var taxFinance = taxableFinance * taxRate / 100;
    var taxTotal = taxCustomer + taxFinance;

    var totalWithTax = totalCharge + row.OrderHed_DocTotalTax;

    var payableCustomer = taxableCustomer + taxCustomer + taxFinance;
    var payableFinance = financeAmt;
    var payableTotal = payableCustomer + payableFinance;

    var downPay = row.OrderHed_xDownPayAmt_c;
    var netPayable = payableCustomer - downPay;

    row.UD40_Number01 = round(customerAmt);
    row.UD40_Number02 = round(totalCharge);
    row.UD40_Number03 = round(taxableCustomer);
    row.OrderHed_xManualTaxableAmt_c = round(taxableFinance);
    row.UD40_Number04 = round(taxableTotal);
    row.UD40_Number05 = round(taxCustomer);
    row.OrderHed_xManualTaxAmt_c = round(taxFinance);
    row.UD40_Number06 = round(taxTotal);
    row.UD40_Number07 = round(totalWithTax);
    row.UD40_Number08 = round(payableCustomer);
    row.UD40_Number09 = round(payableFinance);
    row.UD40_Number10 = round(payableTotal);
    row.UD40_Number11 = round(netPayable);

    updateOrder();
}
else if (oldTaxableAmt != newTaxableAmt && newTaxableAmt == 0)
{
    row.OrderHed_xManualTaxAmt_c = 0;

    //row.UD40_Number03 = (row.OrderHed_DocTotalCharges * row.Calculated_DocTaxPercentage / 100) - row.OrderHed_xManualTaxableAmt_c;
    row.UD40_Number03 = round(row.Calculated_TotalTaxableAmt - row.OrderHed_xManualTaxableAmt_c);
    row.UD40_Number05 = round((row.Calculated_TotalTaxableAmt * row.Calculated_DocTaxPercentage / 100) - row.OrderHed_xManualTaxAmt_c);
    //row.UD40_Number09 = round(row.OrderHed_xManualTaxableAmt_c + row.OrderHed_xManualTaxAmt_c);
    row.UD40_Number09 = round(newFinanceAmt + row.OrderHed_xManualTaxAmt_c);


    var taxableTotal = row.UD40_Number03 + row.OrderHed_xManualTaxableAmt_c;
    var taxTotal = row.UD40_Number05 + row.OrderHed_xManualTaxAmt_c;
    var totalWithTax = row.OrderHed_DocTotalCharges + taxTotal;
    //var payableCustomer = row.UD40_Number03 + row.UD40_Number05;
    var payableCustomer = row.OrderHed_DocTotalCharges - newFinanceAmt + row.UD40_Number05;  
    var payableTotal = payableCustomer + row.UD40_Number09;
    var netPayable = payableCustomer - row.OrderHed_xDownPayAmt_c;

    row.UD40_Number04 = round(taxableTotal);
    row.UD40_Number06 = round(taxTotal);
    row.UD40_Number07 = round(totalWithTax);
    row.UD40_Number08 = round(payableCustomer);
    row.UD40_Number10 = round(payableTotal);
    row.UD40_Number11 = round(netPayable);

    updateOrder();
}
else if (oldTaxableAmt != newTaxableAmt && newTaxableAmt != 0)
{
    var taxFinance = row.OrderHed_xManualTaxableAmt_c * row.Calculated_DocTaxPercentage / 100;
    row.OrderHed_xManualTaxAmt_c = round(taxFinance);

    //row.UD40_Number03 = (row.OrderHed_DocTotalCharges * row.Calculated_DocTaxPercentage / 100) - row.OrderHed_xManualTaxableAmt_c;
    row.UD40_Number03 = round(row.Calculated_TotalTaxableAmt - row.OrderHed_xManualTaxableAmt_c);
    row.UD40_Number05 = round((row.Calculated_TotalTaxableAmt * row.Calculated_DocTaxPercentage / 100) - row.OrderHed_xManualTaxAmt_c);
    //row.UD40_Number09 = round(row.OrderHed_xManualTaxableAmt_c + row.OrderHed_xManualTaxAmt_c);
    row.UD40_Number09 = round(newFinanceAmt + row.OrderHed_xManualTaxAmt_c);


    var taxableTotal = row.UD40_Number03 + row.OrderHed_xManualTaxableAmt_c;
    var taxTotal = row.UD40_Number05 + row.OrderHed_xManualTaxAmt_c;
    var totalWithTax = row.OrderHed_DocTotalCharges + taxTotal;
    //var payableCustomer = row.UD40_Number03 + row.UD40_Number05;
    var payableCustomer = row.OrderHed_DocTotalCharges - newFinanceAmt + row.UD40_Number05;  
    var payableTotal = payableCustomer + row.UD40_Number09;
    var netPayable = payableCustomer - row.OrderHed_xDownPayAmt_c;

    row.UD40_Number04 = round(taxableTotal);
    row.UD40_Number06 = round(taxTotal);
    row.UD40_Number07 = round(totalWithTax);
    row.UD40_Number08 = round(payableCustomer);
    row.UD40_Number10 = round(payableTotal);
    row.UD40_Number11 = round(netPayable);

    updateOrder();
}
else if (oldTaxAmt != newTaxAmt && newTaxAmt == 0)
{
    row.OrderHed_xManualTaxableAmt_c = 0;

    //row.UD40_Number03 = (row.OrderHed_DocTotalCharges * row.Calculated_DocTaxPercentage / 100) - row.OrderHed_xManualTaxableAmt_c;
    row.UD40_Number03 = round(row.Calculated_TotalTaxableAmt - row.OrderHed_xManualTaxableAmt_c);
    row.UD40_Number05 = round((row.Calculated_TotalTaxableAmt * row.Calculated_DocTaxPercentage / 100) - row.OrderHed_xManualTaxAmt_c);
    //row.UD40_Number09 = round(row.OrderHed_xManualTaxableAmt_c + row.OrderHed_xManualTaxAmt_c);
    row.UD40_Number09 = round(newFinanceAmt + row.OrderHed_xManualTaxAmt_c);


    var taxableTotal = row.UD40_Number03 + row.OrderHed_xManualTaxableAmt_c;
    var taxTotal = row.UD40_Number05 + row.OrderHed_xManualTaxAmt_c;
    var totalWithTax = row.OrderHed_DocTotalCharges + taxTotal;
    //var payableCustomer = row.UD40_Number03 + row.UD40_Number05;
    var payableCustomer = row.OrderHed_DocTotalCharges - newFinanceAmt + row.UD40_Number05;  
    var payableTotal = payableCustomer + row.UD40_Number09;
    var netPayable = payableCustomer - row.OrderHed_xDownPayAmt_c;

    row.UD40_Number04 = round(taxableTotal);
    row.UD40_Number06 = round(taxTotal);
    row.UD40_Number07 = round(totalWithTax);
    row.UD40_Number08 = round(payableCustomer);
    row.UD40_Number10 = round(payableTotal);
    row.UD40_Number11 = round(netPayable);

    updateOrder();
}
else if (oldTaxAmt != newTaxAmt && newTaxAmt != 0)
{
    row.OrderHed_xManualTaxableAmt_c = round(newTaxAmt / row.Calculated_DocTaxPercentage * 100);

    //row.UD40_Number03 = (row.OrderHed_DocTotalCharges * row.Calculated_DocTaxPercentage / 100) - row.OrderHed_xManualTaxableAmt_c;
    row.UD40_Number03 = round(row.Calculated_TotalTaxableAmt - row.OrderHed_xManualTaxableAmt_c);
    row.UD40_Number05 = round((row.Calculated_TotalTaxableAmt * row.Calculated_DocTaxPercentage / 100) - row.OrderHed_xManualTaxAmt_c);
    //row.UD40_Number09 = round(row.OrderHed_xManualTaxableAmt_c + row.OrderHed_xManualTaxAmt_c);
    row.UD40_Number09 = round(newFinanceAmt + row.OrderHed_xManualTaxAmt_c);


    var taxableTotal = row.UD40_Number03 + row.OrderHed_xManualTaxableAmt_c;
    var taxTotal = row.UD40_Number05 + row.OrderHed_xManualTaxAmt_c;
    var totalWithTax = row.OrderHed_DocTotalCharges + taxTotal;
    //var payableCustomer = row.UD40_Number03 + row.UD40_Number05;
    var payableCustomer = row.OrderHed_DocTotalCharges - newFinanceAmt + row.UD40_Number05;  
    var payableTotal = payableCustomer + row.UD40_Number09;
    var netPayable = payableCustomer - row.OrderHed_xDownPayAmt_c;

    row.UD40_Number04 = round(taxableTotal);
    row.UD40_Number06 = round(taxTotal);
    row.UD40_Number07 = round(totalWithTax);
    row.UD40_Number08 = round(payableCustomer);
    row.UD40_Number10 = round(payableTotal);
    row.UD40_Number11 = round(netPayable);

    updateOrder();
}
else if (oldFinanceAmt != newFinanceAmt)
{
    row.OrderHed_xManualTax_c = false;

    row.OrderHed_xFinanceAmt_c = row.OrderHed_xFinanceAmt_c > row.OrderHed_DocTotalCharges ? row.OrderHed_DocTotalCharges : row.OrderHed_xFinanceAmt_c;
    
    var totalCharge = row.OrderHed_DocTotalCharges;
    var financeAmt = row.OrderHed_xFinanceAmt_c;
    var customerAmt = totalCharge - financeAmt;

    var taxRate = row.Calculated_DocTaxPercentage;

    var taxableFinance = financeAmt > row.Calculated_TotalTaxableAmt ? row.Calculated_TotalTaxableAmt : financeAmt;
    var taxableCustomer = row.Calculated_TotalTaxableAmt - taxableFinance;
    var taxableTotal = taxableCustomer + taxableFinance;
    
    var taxFinance = taxableFinance * taxRate / 100;
    var taxCustomer = taxableCustomer * taxRate / 100;
    var taxTotal = taxCustomer + taxFinance;

    var totalWithTax = totalCharge + row.OrderHed_DocTotalTax;
    
    var payableCustomer = customerAmt + taxCustomer;
    var payableFinance = financeAmt + taxFinance;
    var payableTotal = payableCustomer + payableFinance;

    var downPay = row.OrderHed_xDownPayAmt_c;
    var netPayable = payableCustomer - downPay;

    row.UD40_Number01 = round(customerAmt);
    row.UD40_Number02 = round(totalCharge);
    row.UD40_Number03 = round(taxableCustomer);
    row.OrderHed_xManualTaxableAmt_c = round(taxableFinance);
    row.UD40_Number04 = round(taxableTotal);
    row.UD40_Number05 = round(taxCustomer);
    row.OrderHed_xManualTaxAmt_c = round(taxFinance);
    row.UD40_Number06 = round(taxTotal);
    row.UD40_Number07 = round(totalWithTax);
    row.UD40_Number08 = round(payableCustomer);
    row.UD40_Number09 = round(payableFinance);
    row.UD40_Number10 = round(payableTotal);
    row.UD40_Number11 = round(netPayable);

    updateOrder();
    
}
