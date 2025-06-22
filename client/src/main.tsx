import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import {createBrowserRouter, RouterProvider} from "react-router-dom";

const router = createBrowserRouter([
  {
    path: "/",
    //element: <Layout />,
    children: [
      {
        index: true,
        //element: <SignOnPage />,
      },
      {
        path: "posts/schedule",
        //element: <TelegramChannelSchedulePage />,
      },
    ],
    errorElement: <div>Page not found</div>,
  },
])

createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <RouterProvider router={router} />
    </StrictMode>,
)
