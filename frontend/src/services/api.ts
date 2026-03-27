import axios from "axios";

const api = axios.create({
  baseURL: "https://cloud-task-manager-api-96346-dtewbcgxf6g8gzg7.germanywestcentral-01.azurewebsites.net/api", 
});

export default api