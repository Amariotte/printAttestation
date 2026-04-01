using InteroperabiliteProject.Model;
using InteroperabiliteProject.RequestToReceiveDto;

namespace InteroperabiliteProject.Interface
{
    public interface IdatasRepo : IbaseRepo<t_datas>
    {
        public Task<Dictionary<string, string>> getDataInDictionaryByCode(string code);


        public Task<List<T>> getDataInListByCode<T>(string code);

        public Task<string> getItemDescriptionByCodeAndKey(string code_item, string value_key_search);
       

    }
}
