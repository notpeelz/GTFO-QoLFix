import { readFile } from "fs/promises";
import { Parser } from "xml2js";

export default class Csproj {
  private XmlData: any;

  static async fromPath(path: string): Promise<Csproj> {
    const data = await readFile(path, "utf8");
    const parser = new Parser();
    const csproj = new Csproj();
    csproj.XmlData = await parser.parseStringPromise(data);
    return csproj;
  }

  findPropertyGroup(name: string) {
    const prop = this.XmlData.Project.PropertyGroup.find(
      (x: Record<string, string>) => x[name] != null,
    );
    if (prop == null) return null;
    return prop[name];
  }

  getPropertyValue(name: string) {
    const prop = this.findPropertyGroup(name);

    if (prop == null || prop.length === 0) {
      throw new Error(`Missing MSBuild property: ${name}`);
    }

    if (prop.length > 1) {
      throw new Error(`MSBuild property was defined more than once: ${name}`);
    }

    return prop[0];
  }
}
