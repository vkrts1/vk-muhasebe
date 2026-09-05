import { create } from 'zustand';

interface UiState {
  isGlobalLoading: boolean;
  setGlobalLoading: (loading: boolean) => void;
  
  // Masaüstü (Avalonia) MainViewModel eşleniği
  isMenuOpen: boolean;
  toggleMenu: () => void;
}

export const useUiStore = create<UiState>((set) => ({
  isGlobalLoading: false,
  setGlobalLoading: (loading) => set({ isGlobalLoading: loading }),
  
  isMenuOpen: false,
  toggleMenu: () => set((state) => ({ isMenuOpen: !state.isMenuOpen })),
}));

// Gelecekte FinansViewModel, CariViewModel vb. buraya eklenecek
