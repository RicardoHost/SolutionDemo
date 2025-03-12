using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CostCalculationQuota.Unitys.unitity
{
    public class SimpleUnitPriceHelper
    {
        public void CalculateTaxPriceByNoTaxPrice<T>(T item) where T : ISimpleUnitPrice
        {
            var csdj_decimal = Decimal.Parse(item.csdj);
            var slv_decimal = Decimal.Parse(item.slv);
            var hsdj_decimal = csdj_decimal * (1+slv_decimal/100);
            item.hsdj = hsdj_decimal.ToString();
        }

        public void CalculateByNoTaxPrice<T>(T item) where T : ISimpleUnitPrice
        {
            CalculateTaxPriceByNoTaxPrice(item);
            CalculateByQuantity(item);
        }

        public void CalculateNoTaxPriceByTaxPrice<T>(T item) where T : ISimpleUnitPrice
        {
            var hsdj_decimal = Decimal.Parse(item.hsdj);
            var slv_decimal = Decimal.Parse(item.slv);
            var csdj_decimal = hsdj_decimal / (1 + slv_decimal / 100);
            item.csdj = csdj_decimal.ToString();
        }

        public void CalculateByTaxPrice<T>(T item) where T : ISimpleUnitPrice
        {
            CalculateTaxPriceByNoTaxPrice(item);
            CalculateByQuantity(item);
        }


        public void CalculateTaxPriceByNoTaxPriceWithFormula<T>(T item) where T : IFormulaUnitPrice
        {
            CalculateTaxPriceByNoTaxPrice(item);
            item.hsdjgs = item.hsdj;
        }

        public void CalculateByNoTaxPriceWithFormula<T>(T item) where T : IFormulaUnitPrice
        {
            CalculateTaxPriceByNoTaxPriceWithFormula(item);
            CalculateByQuantity(item);
        }

        public void CalculateNoTaxPriceByTaxPriceWithFormula<T>(T item) where T : IFormulaUnitPrice
        {
            CalculateNoTaxPriceByTaxPrice(item);
            item.csdjgs = item.csdj;
        }

        public void CalculateByTaxPriceWithFormula<T>(T item) where T : IFormulaUnitPrice
        {
            CalculateTaxPriceByNoTaxPriceWithFormula(item);
            CalculateByQuantity(item);
        }

        public void CalculateByQuantity<T>(T item) where T : ISimpleUnitPrice
        {
            var gcl_decimal = Decimal.Parse(item.gcl);

            if(item is ITimeUnitPrice)
            {
                var sysj_decimal = Decimal.Parse((item as ITimeUnitPrice).sysj);
                gcl_decimal = gcl_decimal * sysj_decimal;
            }

            //计算除税合价 = 除税单价 * 工程量
            var csdj_decimal = Decimal.Parse(item.csdj);
            var cshj_decimal = gcl_decimal * csdj_decimal;
            item.cshj = cshj_decimal.ToString();

            //计算含税合价 = 含税单价 * 工程量
            var hsdj_decimal = Decimal.Parse(item.hsdj);
            var hshj_decimal = gcl_decimal * hsdj_decimal;
            item.hshj = hshj_decimal.ToString();
        }
    }


    public interface IQuantity
    {
        public string gcl { set; get; }
    }

    public interface IQuantityWithFormula : IQuantity
    {
        public string gclgs { set; get; }
    }

    public interface IUseTime
    {


    }

    public interface INoTaxPrice
    {
        public string csdj { set; get; }
    }

    public interface INoTaxPriceWithFormula : INoTaxPrice
    {
        public string csdjgs { set; get; }
    }

    public interface ITaxPrice
    {
        public string hsdj { set; get; }
    }

    public interface ITaxPriceWithFormula : ITaxPrice
    {
        public string hsdjgs { set; get; }
    }


    public interface ISimpleUnitPrice : IQuantity, INoTaxPrice, ITaxPrice
    {
        public string slv { set; get; }
        public string cshj { set; get; }
        public string hshj { set; get; }
    }

    public interface IFormulaUnitPrice : IQuantityWithFormula, INoTaxPriceWithFormula, ITaxPriceWithFormula, ISimpleUnitPrice
    {
        public string gclgs { set; get; }
        public string csdjgs { set; get; }
        public string hsdjgs { set; get; }
    }

    public interface ITimeUnitPrice : IFormulaUnitPrice
    {
        public string sysj { set; get; }
        public string sysjgs { set; get; }
    }

}
