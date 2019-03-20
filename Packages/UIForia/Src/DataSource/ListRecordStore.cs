using UIForia.Util;

namespace UIForia.DataSource {

    public class ListRecordStore<T> : IRecordStore<T> where T : class, IRecord {

        private readonly LightList<T> store;

        public ListRecordStore() {
            store = new LightList<T>(16);
        }

        public int Count => store.Count;

        public T RemoveRecord(int id) {
            int count = store.Count;
            T[] array = store.Array;
            for (int i = 0; i < count; i++) {
                if (array[i].Id == id) {
                    T retn = array[i];
                    store.RemoveAt(i);
                    return retn;
                }
            }

            return null;
        }

        public void SetRecord(T record) {
            int id = record.Id;
            int count = store.Count;
            T[] array = store.Array;
            for (int i = 0; i < count; i++) {
                if (array[i].Id == id) {
                    array[i] = record;
                    return;
                }
            }

            store.Add(record);
        }

        public T GetRecord(int id) {
            int count = store.Count;
            T[] array = store.Array;
            for (int i = 0; i < count; i++) {
                if (array[i].Id == id) {
                    return array[i];
                }
            }

            return null;
        }

    }

}