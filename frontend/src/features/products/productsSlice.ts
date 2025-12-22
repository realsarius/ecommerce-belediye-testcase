import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

interface ProductState {
  page: number;
  search: string;
  categoryId: string;
  sortBy: string;
  sortDesc: boolean;
}

const initialState: ProductState = {
  page: 1,
  search: '',
  categoryId: '',
  sortBy: 'createdAt',
  sortDesc: true,
};

const productsSlice = createSlice({
  name: 'products',
  initialState,
  reducers: {
    setPage: (state, action: PayloadAction<number>) => {
      state.page = action.payload;
    },
    setSearch: (state, action: PayloadAction<string>) => {
      state.search = action.payload;
      state.page = 1; // Reset to page 1 on search
    },
    setCategoryId: (state, action: PayloadAction<string>) => {
      state.categoryId = action.payload;
      state.page = 1; // Reset to page 1 on filter
    },
    setSortBy: (state, action: PayloadAction<string>) => {
      state.sortBy = action.payload;
    },
    setSortDesc: (state, action: PayloadAction<boolean>) => {
      state.sortDesc = action.payload;
    },
    resetFilters: (state) => {
      state.search = '';
      state.categoryId = '';
      state.sortBy = 'createdAt';
      state.sortDesc = true;
      state.page = 1;
    },
  },
});

export const {
  setPage,
  setSearch,
  setCategoryId,
  setSortBy,
  setSortDesc,
  resetFilters,
} = productsSlice.actions;

export default productsSlice.reducer;
